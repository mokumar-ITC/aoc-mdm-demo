using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMItemSupplierRefAttriRepository : GenericRepository<MDMItemSupplierRefAttributes>, IMDMItemSupplierRefAttributesRepository
    {
        public MDMItemSupplierRefAttriRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.item_supplier_reference_attributes", catalog)
        {
        }

    }
}
