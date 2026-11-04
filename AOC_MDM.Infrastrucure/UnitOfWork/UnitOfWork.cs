using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Infrastrucure.Repository;
using System.Data.Odbc;
using AOC_MDM.Core.Entities;

namespace AOC_MDM.Infrastrucure.UnitOfWork
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _connectionString;
        private readonly string _catalog;
        private OdbcConnection _connection; // Single connection instance
        private bool _disposed;

        MDMSpendAreaRepository _mDMSpendAreaRepository;
        MDMVendorRefAttriRepository _mDMVendorRefAttriRepository;
        MDMVendorRefAttriAuditRepository _mDMVendorRefAttriAuditRepository;
        MDMTeamRepository _mDMTeamRepository;
        MDMAOCLevel1Repository _mDMAOCLevel1Repository;
        MDMAOCLevel2Repository _mDMAOCLevel2Repository;
        MDMAOCLevel3Repository _mDMAOCLevel3Repository;
        MDMViewVendorRefAttrsRepository _mDMViewVendorRefAttrsRepository;

        MDMItemRefAttriRepository _mDMItemRefAttriRepository;
        MDMItemRefAttriAuditRepository _mDMItemRefAttriAuditRepository;
        MDMViewItemRefAttrsRepository _mDMViewItemRefAttrsRepository;

        MDMItemSupplierRefAttriRepository _mDMItemSupplierRefAttriRepository;
        MDMItemSupplierRefAttriAuditRepository _mDMItemSupplierRefAttriAuditRepository;
        MDMViewItemSupplierRefAttrsRepository _mDMViewItemSupplierRefAttrsRepository;
       
        MDMViewAOCLevel1Repository _mDMViewAOCLevel1Repository;
        MDMViewAOCLevel2Repository _mDMViewAOCLevel2Repository;
        MDMViewAOCLevel3Repository _mDMViewAOCLevel3Repository;

        UserRoleRepository _userRoleRepository;
        HierarchyRepository _hierarchyRepository;
        UserAccountRepository _userAccountRepository;

        public UnitOfWork(string connectionString, string catalog)
        {
            _connectionString = connectionString;
            _catalog = catalog;
        }


        // Lazy loading of a single OdbcConnection
        private OdbcConnection GetOrCreateConnection()
        {
            if (_connection == null)
            {
                _connection = new OdbcConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        public IMDMSpendAreaRepository SpendAreaRepository => _mDMSpendAreaRepository ??= new MDMSpendAreaRepository(GetOrCreateConnection(), _catalog);

        public IMDMTeamRepository TeamRepository => _mDMTeamRepository ??= new MDMTeamRepository(GetOrCreateConnection(),_catalog);

        public IMDMAOCLevel1Repository AOCLevel1Repository => _mDMAOCLevel1Repository ??= new MDMAOCLevel1Repository(GetOrCreateConnection(),_catalog);

        public IMDMAOCLevel2Repository AOCLevel2Repository => _mDMAOCLevel2Repository ??= new MDMAOCLevel2Repository(GetOrCreateConnection(),_catalog);

        public IMDMAOCLevel3Repository AOCLevel3Repository => _mDMAOCLevel3Repository ??= new MDMAOCLevel3Repository(GetOrCreateConnection(),_catalog);

        public IMDMVendorRefAttributesRepository VendorRefAttributesRepository => _mDMVendorRefAttriRepository ??= new MDMVendorRefAttriRepository(GetOrCreateConnection(),_catalog);

        public IMDMVendorRefAttriAuditRepository VendorRefAttriAuditRepository => _mDMVendorRefAttriAuditRepository ??= new MDMVendorRefAttriAuditRepository(GetOrCreateConnection(),_catalog);

        public IMDMViewVendorRefAttrsRepository ViewVendorRefAttrsRepository => _mDMViewVendorRefAttrsRepository ??= new MDMViewVendorRefAttrsRepository(GetOrCreateConnection(),_catalog);


        public IMDMItemRefAttributesRepository ItemRefAttributesRepository => _mDMItemRefAttriRepository ??= new MDMItemRefAttriRepository(GetOrCreateConnection(),_catalog);

        public IMDMItemRefAttriAuditRepository ItemRefAttriAuditRepository => _mDMItemRefAttriAuditRepository ??= new MDMItemRefAttriAuditRepository(GetOrCreateConnection(),_catalog);


        public IMDMViewItemRefAttrsRepository ViewItemRefAttrsRepository => _mDMViewItemRefAttrsRepository ??= new MDMViewItemRefAttrsRepository(GetOrCreateConnection(),_catalog);

        public IMDMItemSupplierRefAttributesRepository ItemSupplierRefAttributesRepository => _mDMItemSupplierRefAttriRepository ??= new MDMItemSupplierRefAttriRepository(GetOrCreateConnection(),_catalog);

        public IMDMItemSupplierRefAttriAuditRepository ItemSupplierRefAttriAuditRepository => _mDMItemSupplierRefAttriAuditRepository ??= new MDMItemSupplierRefAttriAuditRepository(GetOrCreateConnection(),_catalog);

        public IMDMViewItemSupplierRefAttrsRepository ViewItemSupplierRefAttrsRepository => _mDMViewItemSupplierRefAttrsRepository ??= new MDMViewItemSupplierRefAttrsRepository(GetOrCreateConnection(),_catalog);

        public IMDMViewAOCLevel1Repository ViewAOCLevel1Repository => _mDMViewAOCLevel1Repository ??= new MDMViewAOCLevel1Repository(GetOrCreateConnection(), _catalog);

        public IMDMViewAOCLevel2Repository ViewAOCLevel2Repository => _mDMViewAOCLevel2Repository ??= new MDMViewAOCLevel2Repository(GetOrCreateConnection(), _catalog);

        public IMDMViewAOCLevel3Repository ViewAOCLevel3Repository => _mDMViewAOCLevel3Repository ??= new MDMViewAOCLevel3Repository(GetOrCreateConnection(), _catalog);

        public IUserRoleRepository UserRoleRepository => _userRoleRepository ??= new UserRoleRepository(GetOrCreateConnection(), _catalog);

        public IHierarchyRepository HierarchyRepository => _hierarchyRepository ??= new HierarchyRepository(GetOrCreateConnection(), _catalog);

        public IUserAccountRepository UserAccountRepository => _userAccountRepository ??= new UserAccountRepository(GetOrCreateConnection(), _catalog);


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                }
            }

            _disposed = true;
        }

    }


}
