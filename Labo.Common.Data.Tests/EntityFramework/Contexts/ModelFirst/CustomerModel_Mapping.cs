//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Labo.Common.Data.Tests.EntityFramework.Contexts.ModelFirst
{
    #pragma warning disable 1573
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    using System.Data.Entity.Infrastructure;
    
    internal partial class CustomerModel_Mapping : EntityTypeConfiguration<CustomerModel>
    {
        public CustomerModel_Mapping()
        {                        
              this.HasKey(t => t.Id);        
              this.ToTable("CustomerModelSet");
              this.Property(t => t.Id).HasColumnName("Id");
              this.Property(t => t.Name).HasColumnName("Name").IsRequired();
         }
    }
}

