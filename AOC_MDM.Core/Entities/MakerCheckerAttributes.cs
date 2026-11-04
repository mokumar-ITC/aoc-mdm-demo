using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Core.Entities
{
    public class MakerCheckerAttributes : AuditColumns
    {
        [Key]
        [Column("AuditId")]
        public Int64 AuditId { get; set; }
        public string ChangeType { get; set; } // Insert, Update, Delete
       // public string ApprovalStatus { get; set; } // Pending, Approved, Rejected
        public string MakerId { get; set; }
        public string CheckerId { get; set; }
        public DateTime? ChangeTimestamp { get; set; }
        public DateTime? ApprovalTimestamp { get; set; }
      
        [Column("LastModifiedByRole")]
        public string LastModifiedByRole { get; set; }

        [Column("Remarks")]
        public string Remarks { get; set; }

    }
}
