using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class MDMTeamRepository : GenericRepository<MDMTeam>, IMDMTeamRepository
    {
        public MDMTeamRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.team", catalog)
        {
        }

    }
}
