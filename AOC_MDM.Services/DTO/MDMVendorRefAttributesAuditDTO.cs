using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMVendorRefAttributesAuditDTO : MakerCheckerAttributes
    {
        /// <summary>
        /// Unique identifier for the vendor.
        /// </summary>
        [Required(ErrorMessage = "Vendor Id is required!")]
        [MaxLength(255)]
        public string VendorId { get; set; }

        public string? Source { get; set; }

        /// <summary>
        /// The supplier/vendor name.
        /// </summary>
        public string SupplierVendor { get; set; }

        /// <summary>
        /// The English name of the vendor.
        /// </summary>
            public string VendorEnglishName { get; set; }

        /// <summary>
        /// The family name of the vendor.
        /// </summary>

        public string VendorFamilyName { get; set; }

        /// <summary>
        /// Material type.
        /// </summary>
        public string Level3 { get; set; }

        /// <summary>
        /// The parent company of the vendor.
        /// </summary>
        public string ParentCompany { get; set; }

        /// <summary>
        /// Indicates if the spend is addressable (Yes/No).
        /// </summary>
        public string AddressableSpend { get; set; }

        /// <summary>
        /// Indicates if the vendor is a very important supplier for payables.
        /// </summary>
        public string PayablesVeryImportantSupplier { get; set; }

        /// <summary>
        /// Indicates if the vendor is intercompany (Yes/No).
        /// </summary>
        public string Intercompany { get; set; }

        /// <summary>
        /// The owner of the indirect spend.
        /// </summary>
        public string IndirectSpendOwner { get; set; }
               
        public bool IsActive { get; set; }


        //ApprovalStatus is nothing bu UpdateStatus in main table
        //[Column("ApprovalStatus")]
        public string ApprovalStatus { get; set; } // Pending, Approved, Rejected

    }
}
