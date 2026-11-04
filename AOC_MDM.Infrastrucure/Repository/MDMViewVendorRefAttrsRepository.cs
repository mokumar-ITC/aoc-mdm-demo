using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMViewVendorRefAttrsRepository : GenericRepository<MDMViewVendorRefAttrs>, IMDMViewVendorRefAttrsRepository
    {
        public MDMViewVendorRefAttrsRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.vw_vendor_reference_attributes", catalog)
        {
        }

    }
}
