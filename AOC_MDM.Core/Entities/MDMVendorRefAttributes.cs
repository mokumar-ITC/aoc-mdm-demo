using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
   // [Table("vendor_reference_attributes", Schema = "mdm_sandbox.mdm")] // Specify the schema if needed
    public class MDMVendorRefAttributes : AuditColumns
    {
        /// <summary>
        /// Unique identifier for the vendor.
        /// </summary>
        [Key]
        [Column("vendor_id")]
        public string VendorId { get; set; }

        [Column("source")]
        public string? Source { get; set; }

        /// <summary>
        /// The supplier/vendor name.
        /// </summary>
        [Column("supplier_vendor")]
        public string SupplierVendor { get; set; }

        /// <summary>
        /// The English name of the vendor.
        /// </summary>
        [Column("vendor_english_name")]
        public string VendorEnglishName { get; set; }

        /// <summary>
        /// The family name of the vendor.
        /// </summary>
        [Column("vendor_family_name")]
        public string VendorFamilyName { get; set; }

        /// <summary>
        /// Material type.
        /// </summary>
        [Column("level_3")]
        public string Level3 { get; set; }

        /// <summary>
        /// The parent company of the vendor.
        /// </summary>
        [Column("parent_company")]
        public string ParentCompany { get; set; }

        /// <summary>
        /// Indicates if the spend is addressable (Yes/No).
        /// </summary>
        [Column("addressable_spend")]
        public string AddressableSpend { get; set; }

        /// <summary>
        /// Indicates if the vendor is a very important supplier for payables.
        /// </summary>
        [Column("payables_very_important_supplier")]
        public string PayablesVeryImportantSupplier { get; set; }

        /// <summary>
        /// Indicates if the vendor is intercompany (Yes/No).
        /// </summary>
        [Column("intercompany")]
        public string Intercompany { get; set; }

        /// <summary>
        /// The owner of the indirect spend.
        /// </summary>
        [Column("indirect_spend_owner")]
        public string IndirectSpendOwner { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        //UpdateStatus is nothing but ApprovalStatus in Audit table
        [Column("UpdateStatus")]
        public string UpdateStatus { get; set; } // Pending, Approved, Rejected

        [Column("LastModifiedByRole")]
        public string LastModifiedByRole { get; set; }

        [Column("Remarks")]
        public string Remarks { get; set; }

    }
}
