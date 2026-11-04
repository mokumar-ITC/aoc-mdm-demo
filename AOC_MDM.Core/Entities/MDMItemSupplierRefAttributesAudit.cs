using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
   // [Table("item_supplier_reference_attributes_audit", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMItemSupplierRefAttributesAudit : MakerCheckerAttributes
    {
      
        [Column("source")]
        public string Source { get; set; }

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
               
        [Column("IsActive")]
        public bool IsActive { get; set; }

      
        [Column("ApprovalStatus")]
        public string ApprovalStatus { get; set; }
                
    }
}
