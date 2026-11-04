using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMVendorRefAttriRepository : GenericRepository<MDMVendorRefAttributes>, IMDMVendorRefAttributesRepository
    {
        public MDMVendorRefAttriRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.vendor_reference_attributes", catalog) {
        }

    }
}
