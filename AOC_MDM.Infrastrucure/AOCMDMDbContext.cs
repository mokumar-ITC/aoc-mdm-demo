using Microsoft.EntityFrameworkCore;
using AOC_MDM.Core.Entities;
using System.Data.Odbc;
using System;

namespace AOC_MDM.Infrastrucure
{

    public class AOCMDMDbContext : DbContext
    {
        public AOCMDMDbContext(DbContextOptions<AOCMDMDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //string dsn = "dsn=Databricks"; // Replace with your actual DSN
            //optionsBuilder.UseOdbc(dsn);
        }

        public virtual DbSet<MDMSpendArea> SpendArea { get; set; }

        public virtual DbSet<MDMVendorRefAttributes> MDMVendorRefAttributes { get; set; }

        public virtual DbSet<MDMVendorRefAttributesAudit> MDMVendorRefAttributesAudit { get; set; }

        public virtual DbSet<MDMTeam> Team { get; set; }

        public virtual DbSet<MDMAOCLevel1> AOCLevel1 { get; set; }

        public virtual DbSet<MDMAOCLevel2> AOCLevel2 { get; set; }

        public virtual DbSet<MDMAOCLevel3> AOCLevel3 { get; set; }


        public virtual DbSet<MDMViewVendorRefAttrs> ViewVendorRefAttrs { get; set; }

        public virtual DbSet<MDMViewItemRefAttrs> ViewItemRefAttrs { get; set; }


        public virtual DbSet<MDMItemRefAttributes> MDMItemRefAttributes { get; set; }

        public virtual DbSet<MDMItemRefAttributesAudit> MDMItemRefAttributesAudit { get; set; }

        public virtual DbSet<MDMViewItemSupplierRefAttrs> ViewItemSupplierRefAttrs { get; set; }

        public virtual DbSet<MDMItemSupplierRefAttributes> MDMItemSupplierRefAttributes { get; set; }

        public virtual DbSet<MDMItemSupplierRefAttributesAudit> MDMItemSupplierRefAttributesAudit { get; set; }

        public virtual DbSet<UserRole> UserRole { get; set; }

        public virtual DbSet<MDMViewAOCLevel1> ViewAOCLevel1 { get; set; }
    }
}