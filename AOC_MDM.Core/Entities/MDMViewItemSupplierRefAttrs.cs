using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
    //  [Table("vw_item_supplier_reference_attributes", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMViewItemSupplierRefAttrs : AuditColumns
    {
        [Key]
        [Column("source")]
        public string Source { get; set; }

        [Key]
        [Column("item_id")]
        public string ItemId { get; set; }

        [Column("vendor_id")]
        public string VendorId { get; set; }

        [Column("risk_transfer")]
        public string RiskTransfer { get; set; }

        [Column("supplier_specific_requirements")]
        public string SupplierSpecificRequirements { get; set; }

        [Column("formula_non_formula")]
        public string FormulaNonFormula { get; set; }

        [Column("vendor_name")]
        public string VendorName { get; set; }

        [Column("vendor_number")]
        public string VendorNumber { get; set; }

        [Column("item_no")]
        public string ItemNo { get; set; }

        [Column("item_description")]
        public string ItemDescription { get; set; }

        [Column("level_3")]
        public string Level3 { get; set; }

        [Column("mdm_level_2")]
        public string MDMLevel2 { get; set; }

        [Column("mdm_level_1")]
        public string MDMLevel1 { get; set; }

        [Column("team")]
        public string Team { get; set; }

        [Column("spend_area")]
        public string SpendArea { get; set; }

     
        [Column("IsActive")]
        public bool IsActive { get; set; }

        [Column("UpdateStatus")]
        public string UpdateStatus { get; set; }

        [Column("LastModifiedByRole")]
        public string LastModifiedByRole { get; set; }

        [Column("Remarks")]
        public string Remarks { get; set; }



    }

}



