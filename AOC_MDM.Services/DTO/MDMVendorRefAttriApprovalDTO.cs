using AOC_MDM.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOC_MDM.Services.DTO
{
    public class MDMVendorRefAttriApprovalDTO 
    {
      public MDMVendorRefAttributes MainTable {  get; set; }

        public MDMVendorRefAttributesAudit AuditTable { get; set; } 

    }
}
