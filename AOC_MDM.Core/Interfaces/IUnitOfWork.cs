namespace AOC_MDM.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IMDMSpendAreaRepository SpendAreaRepository { get; }

        IMDMTeamRepository TeamRepository { get; }

        IMDMAOCLevel1Repository AOCLevel1Repository { get; }

        IMDMAOCLevel2Repository AOCLevel2Repository { get; }

        IMDMAOCLevel3Repository AOCLevel3Repository { get; }

        IMDMViewVendorRefAttrsRepository ViewVendorRefAttrsRepository { get; }

 


        IMDMVendorRefAttributesRepository VendorRefAttributesRepository { get; }

        IMDMVendorRefAttriAuditRepository VendorRefAttriAuditRepository { get; }

        IMDMItemRefAttributesRepository ItemRefAttributesRepository { get; }

        IMDMItemRefAttriAuditRepository ItemRefAttriAuditRepository { get; }

        IMDMViewItemRefAttrsRepository ViewItemRefAttrsRepository { get; }


        IMDMItemSupplierRefAttributesRepository ItemSupplierRefAttributesRepository { get; }

        IMDMItemSupplierRefAttriAuditRepository ItemSupplierRefAttriAuditRepository { get; }

        IMDMViewItemSupplierRefAttrsRepository ViewItemSupplierRefAttrsRepository { get; }

        IMDMViewAOCLevel1Repository ViewAOCLevel1Repository { get; }

        IMDMViewAOCLevel2Repository ViewAOCLevel2Repository { get; }

        IMDMViewAOCLevel3Repository ViewAOCLevel3Repository { get; }

        IUserRoleRepository UserRoleRepository { get; }
        IHierarchyRepository HierarchyRepository { get; }
        IUserAccountRepository UserAccountRepository { get; }
        IColumnEditSettingRepository ColumnEditSettingRepository { get; }
        IProductMasterRepository ProductMasterRepository { get; }
    }
}
