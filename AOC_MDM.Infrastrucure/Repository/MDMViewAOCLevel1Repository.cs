using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMViewAOCLevel1Repository : GenericRepository<MDMViewAOCLevel1>, IMDMViewAOCLevel1Repository
    {
        public MDMViewAOCLevel1Repository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.vw_aoc_level_1", catalog)
        {
        }

    }
}
