using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
  //  [Table("vw_vendor_reference_attributes", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMViewAOCLevel1 : AuditColumns
    {

        [Key]
        [Column("level_1")]
        public string Level1 { get; set; } // Assuming it's the primary key

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


