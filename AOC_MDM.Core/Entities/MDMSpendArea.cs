using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
   // [Table("spend_area", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMSpendArea :AuditColumns
    {
        [Key] 
        [Column("spend_area")] 
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
    }
}
