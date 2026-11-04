using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMItemRefAttriApprovalDTO 
    {
      public MDMItemRefAttributes MainTable {  get; set; }

        public MDMItemRefAttributesAudit AuditTable { get; set; } 

    }
}
