using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
    //  [Table("vw_aoc_level_2", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMViewAOCLevel2 : AuditColumns
    {
        [Key]
        [Column("level_2")]
        public string Level2 { get; set; } // Assuming it's the primary key

        [Column("level_1")]
        public string Level1 { get; set; }

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


