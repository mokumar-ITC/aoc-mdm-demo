using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMViewItemRefAttrsDTO : AuditColumns
    {
        public string Source { get; set; }
        public string ItemId { get; set; }
        public string ItemEnglishName { get; set; }
        public string CasNumber { get; set; }
        public string ItemForm { get; set; } // e.g., flake, liquid, molten, pellet, powder, solid
        public string Level3 { get; set; } // Material type
        public string AddressableSpend { get; set; } // Yes/No
        public string MaterialCriticalityBasedOnRevenue { get; set; } // high/medium/low
        public string CurrentWavePlan { get; set; } // Yes/No

        public string ItemNo { get; set; }
        public string ItemDescription { get; set; }

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
