using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
    //  [Table("vw_aoc_level_2", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMViewAOCLevel3 : AuditColumns
    {
        [Key]
        [Column("level_3")]
        [Required]
        public string Level3 { get; set; } // Assuming it's the primary key

        [Column("level_2")]
        [Required]
        public string Level2 { get; set; }

        [Column("level_1")]
        public string Level1 { get; set; }

        [Column("team")]
        public string Team { get; set; }

        [Column("spend_area")]
        public string SpendArea { get; set; }

        [Column("feedstock_1")]
        public string Feedstock1 { get; set; }

        [Column("feedstock_2")]
        public string Feedstock2 { get; set; }

        [Column("feedstock_3")]
        public string Feedstock3 { get; set; }

        [Column("hazardous")]
        public string Hazardous { get; set; }

        [Column("conflict_mineral_forced_labor_reg_check")]
        public string ConflictMineralForcedLaborRegCheck { get; set; }

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


