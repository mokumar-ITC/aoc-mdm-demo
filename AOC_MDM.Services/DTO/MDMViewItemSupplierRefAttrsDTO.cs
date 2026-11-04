using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMViewItemSupplierRefAttrsDTO : AuditColumns
    {

        public string Source { get; set; }
        public string ItemId { get; set; }

        public string VendorId { get; set; }
        public string RiskTransfer { get; set; }

        public string SupplierSpecificRequirements { get; set; }
        public string FormulaNonFormula { get; set; }

        public string? VendorName { get; set; }
        public string VendorNumber { get; set; }

        public string ItemNo { get; set; }

        public string ItemDescription { get; set; }
        public string Level3 { get; set; }
        public string MDMLevel2 { get; set; }
        public string MDMLevel1 { get; set; }
        public string Team { get; set; }
        public string SpendArea { get; set; }

        public bool IsActive { get; set; }
        public string UpdateStatus { get; set; }

        public string LastModifiedByRole { get; set; }

        public string Remarks { get; set; }

        public bool IsSelected { get; set; }
    }

    public class ItemBulkUpdateViewModel
    {
        public List<MDMViewItemSupplierRefAttrsDTO> Items { get; set; }
    }
}
