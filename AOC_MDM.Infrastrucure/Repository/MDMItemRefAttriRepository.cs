using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMItemRefAttriRepository : GenericRepository<MDMItemRefAttributes>, IMDMItemRefAttributesRepository
    {
        public MDMItemRefAttriRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.item_reference_attributes", catalog)
        {
        }

    }
}
