using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM_MetaData_GUI.Models
{
    public class AOCLevel3ViewModel
    {

        [Required(ErrorMessage = "Level_3 is required!")]
        [MaxLength(255)]
        [Column("level_3")]
        public string Level3 { get; set; } // Assuming it's the primary key

        [Required(ErrorMessage = "Level_2 is required!")]
        [MaxLength(255)]
        [Column("level_2")]
        public string Level2 { get; set; }

        [Column("feedstock_1")]
        [MaxLength(255)]
        public string Feedstock1 { get; set; }

        [Column("feedstock_2")]
        [MaxLength(255)]
        public string Feedstock2 { get; set; }

        [Column("feedstock_3")]
        [MaxLength(255)]
        public string Feedstock3 { get; set; }

      
        public string Hazardous { get; set; }

        
        public string ConflictMineralForcedLaborRegCheck { get; set; }
        [Column("enabled")]
        public string Enabled { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        public IEnumerable<string> lst_Level { get; set; }
        public string originalLevel2 { get; set; }
        public string Id { get; set; }
        public int TotalNoOfAffectedRecsOfVendor { get; set; }

        public int TotalNoOfAffectedRecsOfItem { get; set; }
        public string Level1 { get; set; }
        public string SpendArea { get; set; }
        public string Team { get; set; }
    }
}
