using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class UserRoleDTO
    {
        public string Login { get; set; }
        public int LevelNo { get; set; }
        public string LevelDesc { get; set; }
    }
}
