using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
  //  [Table("vw_item_reference_attributes", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMViewItemRefAttrs : AuditColumns
    {
        [Column("source")]
        [MaxLength(255)] // Assuming a maximum length for string columns
        public string Source { get; set; }

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

        [Column("item_no")]
        [MaxLength(255)]
        public string ItemNo { get; set; }

        [Column("item_description")]
        [MaxLength(255)]
        public string ItemDescription { get; set; }

        [Column("mdm_level_2")]
        public string Level2 { get; set; }

        [Column("mdm_level_1")]
        public string Level1 { get; set; }

        [Column("team")]
        public string Team { get; set; }

        [Column("spend_area")]
        public string SpendArea { get; set; }


        [Column("IsActive")]
        public bool? IsActive { get; set; }

        public string UpdateStatus { get; set; }
        public string LastModifiedByRole { get; set; }
        public string Remarks { get; set; }

    


    }

}



