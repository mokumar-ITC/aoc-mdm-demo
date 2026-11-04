using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMItemRefAttriDTO : AuditColumns
    {

        public string Source { get; set; }

        [MaxLength(255)]
        public string ItemId { get; set; }

        [Required(ErrorMessage = "Item English is required!")]
        public string ItemEnglishName { get; set; }

        public string CasNumber { get; set; }

        public string ItemForm { get; set; }

        public string Level3 { get; set; }

        public string AddressableSpend { get; set; }

        public string MaterialCriticalityBasedOnRevenue { get; set; }

        public string CurrentWavePlan { get; set; }

        public bool? IsActive { get; set; }

        public string UpdateStatus { get; set; }

        public string LastModifiedByRole { get; set; }

        public string Remarks { get; set; }

    }
}
