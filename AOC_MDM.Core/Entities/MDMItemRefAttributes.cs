using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
   /// <summary>
   /// [Table("item_reference_attributes", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
   /// </summary>
    public class MDMItemRefAttributes : AuditColumns
    {
        [Key]
        [Column("source")]
        [MaxLength(255)] // Assuming a maximum length for strings
        public string Source { get; set; }

        [Key]
        [Column("item_id")]
        [MaxLength(255)]
        public string ItemId { get; set; }

        [Column("item_english_name")]
        [MaxLength(255)]
        public string ItemEnglishName { get; set; }

        [Column("cas_number")]
        [MaxLength(255)]
        public string CasNumber { get; set; }

        [Column("item_form")]
        [MaxLength(255)]
        public string ItemForm { get; set; }

        [Column("level_3")]
        [MaxLength(255)]
        public string Level3 { get; set; }

        [Column("addressable_spend")]
        [MaxLength(255)]
        public string AddressableSpend { get; set; }

        [Column("material_criticality_based_on_revenue")]
        [MaxLength(255)]
        public string MaterialCriticalityBasedOnRevenue { get; set; }

        [Column("current_wave_plan")]
        [MaxLength(255)]
        public string CurrentWavePlan { get; set; }

        [Column("IsActive")]
        public bool? IsActive { get; set; }

        [Column("UpdateStatus")]
        [MaxLength(255)]
        public string UpdateStatus { get; set; }

        [Column("LastModifiedByRole")]
        [MaxLength(255)]
        public string LastModifiedByRole { get; set; }

        [Column("Remarks")]
        [MaxLength(255)]
        public string Remarks { get; set; }


    }
}
