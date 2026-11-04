using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM_MetaData_GUI.Models
{
    public class AOCLevel1ViewModel
    {
        [Required(ErrorMessage = "Level_1 is required!")]
        [MaxLength(255)]
        [Column("level_1")]
        public string Level1 { get; set; } // Assuming it's the primary key

        [Required(ErrorMessage = "Team is required!")]
        [MaxLength(255)]
        [Column("team")]
        public string Team { get; set; }


        [Column("enabled")]
        public string Enabled { get; set; }

        public IEnumerable<string> lst_Team { get; set; }
        public string originalTeam { get; set; }
        public string Id { get; set; }

        public string SpendArea { get; set; }
        public int TotalNoOfAffectedRecsOfVendor { get; set; }

        public int TotalNoOfAffectedRecsOfItem { get; set; }
    }
}
