using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
   // [Table("team", Schema = "mdm_sandbox.mdm")]
    public class MDMTeam : AuditColumns
    {
        [Key] // Assuming Team is the primary key
        [Column("team")]
        public string Team { get; set; }

        [Column("spend_area")]
        public string SpendArea { get; set; }

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
