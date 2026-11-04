using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMViewAOCLevel2DTO : AuditColumns
    {
        public string Level2 { get; set; } // Assuming it's the primary key

        public string Level1 { get; set; }

        public string Team { get; set; }

        public string SpendArea { get; set; }

        public string Enabled { get; set; }

        public bool IsActive { get; set; }

        public string UpdateStatus { get; set; } // Pending, Approved, Rejected

        public string LastModifiedByRole { get; set; }

        public string Remarks { get; set; }

        public bool IsSelected { get; set; }

    }
}
