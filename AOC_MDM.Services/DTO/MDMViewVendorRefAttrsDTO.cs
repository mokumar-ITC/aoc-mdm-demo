using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMViewVendorRefAttrsDTO : AuditColumns
    {
        public string Source { get; set; }
        public string VendorId { get; set; }
        public string SupplierVendor { get; set; }
        public string VendorEnglishName { get; set; }
        public string VendorFamilyName { get; set; }
        public string Level3 { get; set; }
        public string ParentCompany { get; set; }
        public string AddressableSpend { get; set; }
        public string PayablesVeryImportantSupplier { get; set; }
        public string Intercompany { get; set; }
        public string IndirectSpendOwner { get; set; }
        public string VendorName { get; set; }
        public string VendorNumber { get; set; }

        public string Level2 { get; set; }
        public string Level1 { get; set; }

        public string Team { get; set; }

        public string SpendArea { get; set; }

        public bool? IsActive { get; set; }
        public string UpdateStatus { get; set; }
        public string LastModifiedByRole { get; set; }
        public string Remarks { get; set; }
        public bool IsSelected { get; set; }
    }
}
