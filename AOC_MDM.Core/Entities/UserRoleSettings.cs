using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
    public class UserRoleSettings
    {
        public bool EnableRoleCheck { get; set; }
        public HardcodedRole HardcodedRole { get; set; }
    }

    public class HardcodedRole
    {
        public string UserName { get; set; }
        public int LevelNo { get; set; }
    }

}
