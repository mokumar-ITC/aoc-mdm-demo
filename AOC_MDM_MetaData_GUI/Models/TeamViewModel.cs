using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM_MetaData_GUI.Models
{
    public class TeamViewModel
    {
        [Required(ErrorMessage = "Team is required!")]
        [MaxLength(255)]
        [Column("team")]
        public string Team { get; set; }
        [Required(ErrorMessage = "Spend Area is required!")]
        [MaxLength(255)]
        public string SpendArea { get; set; } // Use a more descriptive property name

        [Column("enabled")]
        public string Enabled { get; set; }
        public IEnumerable<string> lst_Team { get; set; }
        public string originalSpendArea { get; set; }
        
        public int TotalNoOfAffectedRecsOfVendor { get; set; }

        public int TotalNoOfAffectedRecsOfItem { get; set; }
    }
}
