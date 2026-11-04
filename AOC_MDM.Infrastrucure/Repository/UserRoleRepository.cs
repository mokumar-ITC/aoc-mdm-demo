using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
    {

        public UserRoleRepository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.intranet.databricks_fe_access_vw", catalog)
        {
        }

    }
}
