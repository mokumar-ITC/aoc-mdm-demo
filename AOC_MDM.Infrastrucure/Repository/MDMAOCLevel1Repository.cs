using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMAOCLevel1Repository : GenericRepository<MDMAOCLevel1>, IMDMAOCLevel1Repository
    {
        public MDMAOCLevel1Repository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.aoc_level_1",catalog)
        {
        }

    }
}
