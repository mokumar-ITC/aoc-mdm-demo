using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMItemSupplierRefAttriAuditRepository : GenericRepository<MDMItemSupplierRefAttributesAudit>, IMDMItemSupplierRefAttriAuditRepository
    {
        public MDMItemSupplierRefAttriAuditRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.item_supplier_reference_attributes_audit", catalog)
        {
        }

    }
}
