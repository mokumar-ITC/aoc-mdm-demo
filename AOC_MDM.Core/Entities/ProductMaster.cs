namespace AOC_MDM.Core.Entities
{
    // mdm.product_hierarchy: Product Family (1) -> Product Type (2) -> Model (3) -> Size (4)
    public class ProductHierarchyNode
    {
        public long NodeId { get; set; }
        public long? ParentNodeId { get; set; }
        public int LevelNo { get; set; }
        public string LevelName { get; set; } = "";
        public string NodeName { get; set; } = "";
        public string NodePath { get; set; } = "";
        public int ProductCount { get; set; } // products attached directly to this node
    }

    // The product columns a user can change. Property names are the column keys used by the
    // column-lock catalog and by the edit form, so keep them identical across the classes below.
    public class ProductMasterValues
    {
        public string ProductName { get; set; } = "";
        public string? ProductCode { get; set; }
        public string? SiopModel { get; set; }
        public string? SiopTier { get; set; }
        public int? SectionsPerUnit { get; set; }
        public decimal? SalePricePerUnit { get; set; }
        public decimal? SalePricePerHour { get; set; }
        public string? DefaultSupplySource { get; set; }
        public decimal? EstFabHours { get; set; }
        public decimal? EstAssemblyHours { get; set; }
        public decimal? FabLbrHrsPct { get; set; }
        public decimal? AssmbLbrHrsPct { get; set; }
        public int? QtyFans { get; set; }
        public int? QtyCoils { get; set; }
        public int? QtyPanels { get; set; }
        public decimal? StandardUnitMultiplier { get; set; }
        public string Status { get; set; } = "";
        public string? CapacityGroup { get; set; }

        public static readonly string[] Statuses = { "Active", "Inactive", "Archived" };

        public ProductMasterValues CopyValues() => new ProductMasterValues().CopyFrom(this);

        public ProductMasterValues CopyFrom(ProductMasterValues source)
        {
            foreach (var p in typeof(ProductMasterValues).GetProperties()) p.SetValue(this, p.GetValue(source));
            return this;
        }

        // Labels of the columns whose value differs from another version of the product
        public IReadOnlyList<string> ChangedFrom(ProductMasterValues other) =>
            typeof(ProductMasterValues).GetProperties()
                .Where(p => !Equals(Normalize(p.GetValue(this)), Normalize(p.GetValue(other))))
                .Select(p => p.Name).ToList();

        private static object? Normalize(object? v) => v switch
        {
            string s when string.IsNullOrWhiteSpace(s) => null,
            string s => s.Trim(),
            _ => v, // decimal equality ignores scale: 85000.00 == 85000
        };
    }

    // mdm.vw_product_master: one product with its hierarchy path and approval state
    public class ProductMasterRecord : ProductMasterValues
    {
        public long ProductId { get; set; }
        public long NodeId { get; set; }
        public string NodePath { get; set; } = "";
        public string ProductFamily { get; set; } = "";
        public string ProductType { get; set; } = "";
        public string? Model { get; set; }
        public string? Size { get; set; }
        public int? SourceRow { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdateStatus { get; set; }        // PendingApproval | Approved | Rejected | UpdateDirectly
        public string? LastModifiedByRole { get; set; }
        public string? Remarks { get; set; }
    }

    // mdm.product_master_audit: one change request (maker / checker)
    public class ProductMasterAudit : ProductMasterValues
    {
        public long AuditId { get; set; }
        public long ProductId { get; set; }
        public string NodePath { get; set; } = "";       // from the product, for display
        public string ChangeType { get; set; } = "Update";
        public string ApprovalStatus { get; set; } = "";
        public string? MakerId { get; set; }
        public string? CheckerId { get; set; }
        public DateTime ChangeTimestamp { get; set; }
        public DateTime? ApprovalTimestamp { get; set; }
        public string? LastModifiedByRole { get; set; }
        public string? Remarks { get; set; }             // maker's reason
        public string? CheckerRemarks { get; set; }
    }

    // mdm.product_master_field: the workbook's field dictionary
    public class ProductMasterField
    {
        public int FieldOrder { get; set; }
        public string SourceField { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string? Description { get; set; }
    }
}
