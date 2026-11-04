using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMAOCLevel3EditDTO : AuditColumns
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

        public int TotalNoOfAffectedRecsOfVendor { get; set; }

        public int TotalNoOfAffectedRecsOfItem { get; set; }
    }
}
