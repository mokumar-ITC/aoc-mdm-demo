using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMItemSupplierRefAttriApprovalDTO 
    {
      public MDMItemSupplierRefAttributes MainTable {  get; set; }

        public MDMItemSupplierRefAttributesAudit AuditTable { get; set; } 

    }
}
