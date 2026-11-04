using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMViewAOCLevel3Repository : GenericRepository<MDMViewAOCLevel3>, IMDMViewAOCLevel3Repository
    {
        public MDMViewAOCLevel3Repository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.vw_aoc_level_3", catalog)
        {
        }

    }
}
