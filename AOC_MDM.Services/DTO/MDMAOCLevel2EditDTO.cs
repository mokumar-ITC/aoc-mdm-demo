using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMAOCLevel2EditDTO : AuditColumns
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


        [Column("UpdateStatus")]
        public string UpdateStatus { get; set; } // Pending, Approved, Rejected

        [Column("LastModifiedByRole")]
        public string LastModifiedByRole { get; set; }

        [Column("Remarks")]
        public string Remarks { get; set; }


        public int TotalNoOfAffectedRecsOfVendor { get; set; }

        public int TotalNoOfAffectedRecsOfItem { get; set; }

    }
}
