using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMViewItemSupplierRefAttrsRepository : GenericRepository<MDMViewItemSupplierRefAttrs>, IMDMViewItemSupplierRefAttrsRepository
    {
        public MDMViewItemSupplierRefAttrsRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.vw_item_supplier_reference_attributes", catalog)
        {
        }

    }
}
