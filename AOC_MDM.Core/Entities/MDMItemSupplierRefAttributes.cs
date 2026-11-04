using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
   // [Table("item_supplier_reference_attributes", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMItemSupplierRefAttributes : AuditColumns
    {
        [Key]
        [Column("item_id")]
        public string ItemId { get; set; } // Assuming it's part of the primary key.

        [Column("source")]
        public string Source { get; set; }

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

        [Column("UpdateStatus")]
        public string UpdateStatus { get; set; }

        [Column("LastModifiedByRole")]
        public string LastModifiedByRole { get; set; }

        [Column("Remarks")]
        public string Remarks { get; set; }

    }
}
