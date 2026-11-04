using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMItemRefAttributesAuditDTO : MakerCheckerAttributes
    {
        public string Source { get; set; }

        public string ItemId { get; set; }

        public string ItemEnglishName { get; set; }

        public string CasNumber { get; set; }

        public string ItemForm { get; set; }

        public string Level3 { get; set; }

        public string AddressableSpend { get; set; }

        public string MaterialCriticalityBasedOnRevenue { get; set; }

        public string CurrentWavePlan { get; set; }

        public bool? IsActive { get; set; }

        //ApprovalStatus is nothing bu UpdateStatus in main table
        public string ApprovalStatus { get; set; } // Pending, Approved, Rejected

        public string Remarks { get; set; }

        public string LastModifiedByRole { get; set; }

    }
}
