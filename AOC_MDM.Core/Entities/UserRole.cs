using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
    public class UserRole
    {
        [Key]
        [Column("LOGIN")]
        [Required]
        public string Login { get; set; }

        [Column("LEVELNO")]
        public int LevelNo { get; set; }

        [Column("LEVELDESC")]
        public string LevelDesc { get; set; }
    }
}
