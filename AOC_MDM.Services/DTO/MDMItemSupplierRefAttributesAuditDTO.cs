using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMItemSupplierRefAttributesAuditDTO : MakerCheckerAttributes
    {

        public string Source { get; set; }
        public string ItemId { get; set; }
        public string VendorId { get; set; }
        public string RiskTransfer { get; set; }
        public string SupplierSpecificRequirements { get; set; }
        public string FormulaNonFormula { get; set; }

        public bool IsActive { get; set; }

        public string ApprovalStatus { get; set; }

    }
}
