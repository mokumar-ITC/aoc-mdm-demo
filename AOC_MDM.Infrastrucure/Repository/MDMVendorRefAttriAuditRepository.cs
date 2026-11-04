using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMVendorRefAttriAuditRepository : GenericRepository<MDMVendorRefAttributesAudit>, IMDMVendorRefAttriAuditRepository
    {
        public MDMVendorRefAttriAuditRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.vendor_reference_attributes_audit", catalog)
        {
        }

    }
}
