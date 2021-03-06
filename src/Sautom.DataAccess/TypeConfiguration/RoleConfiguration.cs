﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Sautom.Domain.Entities;

namespace Sautom.DataAccess.TypeConfiguration
{
    internal sealed class RoleConfiguration : EntityTypeConfiguration<Role>
    {
	    public RoleConfiguration()
        {
            HasKey(rec => rec.Id);
            Property(d => d.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }
    }
}
