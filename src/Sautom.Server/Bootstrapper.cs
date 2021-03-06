﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using AutoMapper;
using Microsoft.Practices.Unity;
using Sautom.DataAccess;
using Sautom.DataAccess.Helpers.Templates;
using Sautom.DataAccess.Queries;
using Sautom.DataAccess.Repositories;
using Sautom.DataAccess.UnitOfWorkAware;
using Sautom.Queries;
using Sautom.Server.Services;
using Sautom.Services.Repositories;
using Sautom.Services.UnitOfWork;

namespace Sautom.Server
{
	internal class Bootstrapper : IDisposable
	{
		private IUnityContainer _configuredContainer;

		private ICollection<ServiceHost> _serviceHosts;

		public Bootstrapper ConfigureApplication()
		{
			string templatesPath = ConfigurationManager.AppSettings["DocxTemplatesFolderPath"];

			TemplateProcessor.Register("ConsultingInfo", new ConsultingInfo(templatesPath));
			TemplateProcessor.Register("WorkshopInfo", new WorkshopInfo(templatesPath));
			TemplateProcessor.Register("OrderDetails", new OrderDetails(templatesPath));

			if (_serviceHosts != null)
			{
				foreach (ServiceHost serviceHost in _serviceHosts)
				{
					if (serviceHost != null && serviceHost.State == CommunicationState.Opened)
					{
						serviceHost.Close();
					}
				}
			}

			IUnityContainer container = InitializeContainer();

			_serviceHosts = new List<ServiceHost>
			{
				new InjectedServiceHost(container, typeof(CommandService)),
				new InjectedServiceHost(container, typeof(QueriesService)),
				new InjectedServiceHost(container, typeof(FileService)),
				new InjectedServiceHost(container, typeof(CommonService)),
				new InjectedServiceHost(container, typeof(ReportService)),
				new InjectedServiceHost(container, typeof(AuthorizationService))
			};

			_configuredContainer = container;

			return this;
		}

		public void RunApplication()
		{
			if (_configuredContainer == null)
				throw new InvalidOperationException("ConfiguredContainer is null. Configure application first.");

			foreach (ServiceHost serviceHost in _serviceHosts) serviceHost.Open();
		}

		#region Initialization

		private IUnityContainer InitializeContainer()
		{
			UnityContainer container = new UnityContainer();

			container.RegisterType<IUnitOfWorkFactory, EfUnitOfWorkFactory>()
				.RegisterType<DatabaseContext, DatabaseContext>(new PerResolveLifetimeManager())
				.RegisterInstance("currentUserName", "pavlova")
				.RegisterInstance(typeof(IMapper), ConfigureMapper());

			ConfigureRepositoriesAndFinders(container);

			return container;
		}

		private IMapper ConfigureMapper()
		{
			IEnumerable<Assembly> assemblies = new[]
			{
				Assembly.GetAssembly(typeof(DataAccess.Mapper.ClientFinder)),
				Assembly.GetAssembly(typeof(Sautom.Services.Mapper.ClientService)),
				Assembly.GetAssembly(typeof(Mapper.AuthorizationService))
			};

			IEnumerable<Profile> profiles = assemblies.SelectMany(a => a.GetTypes()).Where(t =>
				t != typeof(Profile)
				&& typeof(Profile).IsAssignableFrom(t)
				&& !t.IsAbstract
				)
				.Select(Activator.CreateInstance).Cast<Profile>();

			MapperConfiguration config = new MapperConfiguration(cfg =>
			{
				foreach (Profile profile in profiles) cfg.AddProfile(profile);
			});

			return config.CreateMapper();
		}

		private void ConfigureRepositoriesAndFinders(UnityContainer container)
		{
			IEnumerable<Type> reposInterfaces =
				Assembly.GetAssembly(typeof(IRepository<>)).GetExportedTypes().Where(
					rec => rec.Namespace == "Sautom.Services.Repositories" && rec.IsInterface && !rec.IsGenericType);

			IEnumerable<Type> reposImplementation = Assembly.GetAssembly(typeof(RepositoryBase<>)).GetExportedTypes().Where(
				rec => rec.Namespace == "Sautom.DataAccess.Repositories" && rec.IsClass);

			foreach (Type intrface in reposInterfaces)
			{
				Type type = reposImplementation.FirstOrDefault(rec => rec.GetInterface(intrface.Name) != null);
				if (type != null) container.RegisterType(intrface, type);
			}

			IEnumerable<Type> findersInterfaces =
				Assembly.GetAssembly(typeof(IClientFinder)).GetExportedTypes().Where(
					rec => rec.Namespace == "Sautom.Queries" && rec.IsInterface && !rec.IsGenericType);

			IEnumerable<Type> findersImplementation = Assembly.GetAssembly(typeof(FinderBase)).GetExportedTypes().Where(
				rec => rec.Namespace == "Sautom.DataAccess.Queries" && rec.IsClass);

			foreach (Type intrface in findersInterfaces)
			{
				Type type = findersImplementation.FirstOrDefault(rec => rec.GetInterface(intrface.Name) != null);
				if (type != null) container.RegisterType(intrface, type);
			}
		}

		#endregion

		#region IDisposable

		~Bootstrapper()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_serviceHosts != null)
					foreach (ServiceHost serviceHost in _serviceHosts) serviceHost.Close();
			}
		}

		#endregion
	}
}
