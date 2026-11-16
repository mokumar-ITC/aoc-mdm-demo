namespace AOC_MDM.Core.Entities
{
    // One row of mdm.column_edit_setting. A column with no row is editable and uses its built-in label.
    public class ColumnEditSetting
    {
        public string TableKey { get; set; } = "";
        public string ColumnKey { get; set; } = "";
        public bool IsEditable { get; set; } = true;
        public string? DisplayName { get; set; }   // null = built-in label
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
