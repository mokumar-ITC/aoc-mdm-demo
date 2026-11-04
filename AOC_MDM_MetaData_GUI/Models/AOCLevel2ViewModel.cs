using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM_MetaData_GUI.Models
{
    public class AOCLevel2ViewModel
    {
        [Required(ErrorMessage = "Level_2 is required!")]
        [MaxLength(255)]
        [Column("level_2")]
        public string Level2 { get; set; } // Assuming it's the primary key

        [Required(ErrorMessage = "Level_1 is required!")]
        [MaxLength(255)]
        [Column("level_1")]
        public string Level1 { get; set; }


        [Column("enabled")]
        public string Enabled { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        public IEnumerable<string> lst_Level { get; set; }
        public string originalLevel1 { get; set; }
        public string Id { get; set; }
        public string SpendArea { get; set; }
        public string Team { get; set; }
        public int TotalNoOfAffectedRecsOfVendor { get; set; }

        public int TotalNoOfAffectedRecsOfItem { get; set; }
    }
}
