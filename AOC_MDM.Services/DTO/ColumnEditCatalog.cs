namespace AOC_MDM.Services.DTO
{
    public sealed record ManagedColumn(string Key, string Label);

    public sealed record ManagedTable(string Key, string Label, string Group, string Controller,
        IReadOnlyList<ManagedColumn> Columns, string? RenameColumn = null);

    // DisplayName is the admin's label for the column; null means the built-in Column.Label
    public sealed record ColumnEditStatus(ManagedColumn Column, bool IsEditable, string? DisplayName, string? UpdatedBy, DateTime? UpdatedDate)
    {
        public string Label => DisplayName ?? Column.Label;
    }

    public sealed record TableEditStatus(ManagedTable Table, IReadOnlyList<ColumnEditStatus> Columns);

    // The columns an admin can lock. Each Key is the C# property name used by the entities, DTOs and form
    // fields; record identifiers and read-only reference columns are never editable, so they are not listed.
    public static class ColumnEditCatalog
    {
        public const string Vendor = "vendor";
        public const string Item = "item";
        public const string ItemSupplier = "itemsupplier";
        public const string SpendArea = "spendarea";
        public const string Team = "team";
        public const string Level1 = "level1";
        public const string Level2 = "level2";
        public const string Level3 = "level3";
        public const string Product = "product";

        private const string ApprovalGroup = "Reference data (approval workflow)";
        private const string HierarchyGroup = "Hierarchy";
        private const string ProductGroup = "Gradian Product Master (approval workflow)";
        private static readonly ManagedColumn Enabled = new("Enabled", "Enabled");

        public static readonly IReadOnlyList<ManagedTable> Tables = new List<ManagedTable>
        {
            new(Vendor, "Vendor", ApprovalGroup, "VendorRefAttri", new ManagedColumn[]
            {
                new("SupplierVendor", "Supplier Vendor"),
                new("VendorEnglishName", "Vendor English Name"),
                new("VendorFamilyName", "Vendor Family Name"),
                new("Level3", "Level 3"),
                new("ParentCompany", "Parent Company"),
                new("AddressableSpend", "Addressable Spend"),
                new("PayablesVeryImportantSupplier", "Payables Very Important Supplier"),
                new("Intercompany", "Intercompany"),
                new("IndirectSpendOwner", "Indirect Spend Owner"),
            }),
            new(Item, "Item", ApprovalGroup, "ItemRefAttrs", new ManagedColumn[]
            {
                new("ItemEnglishName", "Item English Name"),
                new("CasNumber", "CAS Number"),
                new("ItemForm", "Item Form"),
                new("Level3", "Level 3"),
                new("AddressableSpend", "Addressable Spend"),
                new("MaterialCriticalityBasedOnRevenue", "Material Criticality Based On Revenue"),
                new("CurrentWavePlan", "Current Wave Plan"),
            }),
            new(ItemSupplier, "Item Supplier", ApprovalGroup, "ItemSupplierRefAttri", new ManagedColumn[]
            {
                new("RiskTransfer", "Risk Transfer"),
                new("SupplierSpecificRequirements", "Supplier Specific Requirements"),
                new("FormulaNonFormula", "Formula / Non Formula"),
            }),
            new(SpendArea, "Spend Area", HierarchyGroup, "SpendArea", new ManagedColumn[]
            {
                new("spend_area", "Spend Area name (Rename)"),
                Enabled,
            }, "spend_area"),
            new(Team, "Team", HierarchyGroup, "Team", new ManagedColumn[]
            {
                new("Team", "Team name (Rename)"),
                new("SpendArea", "Spend Area"),
                Enabled,
            }, "Team"),
            new(Level1, "Level 1", HierarchyGroup, "AOCLevel1", new ManagedColumn[]
            {
                new("Level1", "Level 1 name (Rename)"),
                new("Team", "Team"),
                Enabled,
            }, "Level1"),
            new(Level2, "Level 2", HierarchyGroup, "AOCLevel2", new ManagedColumn[]
            {
                new("Level2", "Level 2 name (Rename)"),
                new("Level1", "Level 1"),
                Enabled,
            }, "Level2"),
            new(Level3, "Level 3", HierarchyGroup, "AOCLevel3", new ManagedColumn[]
            {
                new("Level3", "Level 3 name (Rename)"),
                new("Level2", "Level 2"),
                new("Feedstock1", "Feedstock 1"),
                new("Feedstock2", "Feedstock 2"),
                new("Feedstock3", "Feedstock 3"),
                new("Hazardous", "Hazardous"),
                new("ConflictMineralForcedLaborRegCheck", "Conflict Mineral Forced Labor Reg Check"),
                Enabled,
            }, "Level3"),
            new(Product, "Product", ProductGroup, "ProductMaster", new ManagedColumn[]
            {
                new("ProductName", "Product Name"),
                new("ProductCode", "Product Code"),
                new("Status", "Status"),
                new("CapacityGroup", "Capacity Group"),
                new("SiopModel", "SIOP Model"),
                new("SiopTier", "SIOP Tier"),
                new("DefaultSupplySource", "Default Supply Source"),
                new("SectionsPerUnit", "Sections per Unit"),
                new("StandardUnitMultiplier", "Standard Unit Multiplier"),
                new("SalePricePerUnit", "Sale Price per Unit"),
                new("SalePricePerHour", "Sale Price per Hour"),
                new("EstFabHours", "Est. Fabrication Hours"),
                new("EstAssemblyHours", "Est. Assembly Hours"),
                new("FabLbrHrsPct", "Fabrication Labor Hrs %"),
                new("AssmbLbrHrsPct", "Assembly Labor Hrs %"),
                new("QtyFans", "Qty Fans"),
                new("QtyCoils", "Qty Coils"),
                new("QtyPanels", "Qty Panels"),
            }),
        };

        public static ManagedTable? Find(string? tableKey) =>
            Tables.FirstOrDefault(t => string.Equals(t.Key, tableKey, StringComparison.OrdinalIgnoreCase));

        public static ManagedTable? ForController(string? controller) =>
            Tables.FirstOrDefault(t => string.Equals(t.Controller, controller, StringComparison.OrdinalIgnoreCase));
    }
}
