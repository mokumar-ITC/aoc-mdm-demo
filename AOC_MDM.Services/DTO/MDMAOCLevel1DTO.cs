using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMAOCLevel1DTO : AuditColumns
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

        [Column("IsActive")]
        public bool IsActive { get; set; }


        [Column("UpdateStatus")]
        public string UpdateStatus { get; set; } // Pending, Approved, Rejected

        [Column("LastModifiedByRole")]
        public string LastModifiedByRole { get; set; }

        [Column("Remarks")]
        public string Remarks { get; set; }
        public bool IsSelected { get; set; }

    }
}
