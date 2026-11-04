using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMViewAOCLevel2Repository : GenericRepository<MDMViewAOCLevel2>, IMDMViewAOCLevel2Repository
    {
        public MDMViewAOCLevel2Repository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.vw_aoc_level_2", catalog)
        {
        }

    }
}
