using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMSpendAreaDTO : AuditColumns
    {
        [Required(ErrorMessage = "Spend Area is required!")]
        [MaxLength(255)]
        public string spend_area { get; set; } // Use a more descriptive property name

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
