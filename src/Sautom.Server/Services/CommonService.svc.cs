﻿using System.Collections.Generic;
using System.ServiceModel;
using Sautom.Queries;
using Sautom.Server.Interfaces;
using Sautom.Server.TransportDto;

namespace Sautom.Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    sealed public class CommonService : ICommonService
    {
	    #region Constructors

	    public CommonService(ICommonFinder commonFinder)
        {
            CommonFinder = commonFinder;
        }

	    #endregion

	    #region Properties

	    public ICommonFinder CommonFinder { get; set; }

	    #endregion

	    #region Implementation of ICommonService

	    public ICollection<EventNortificationOutput> NortificationList()
        {
            return AutoMapper.Mapper.Map<ICollection<EventNortificationOutput>>(CommonFinder.NortificationList());
        }

	    #endregion
    }
}
