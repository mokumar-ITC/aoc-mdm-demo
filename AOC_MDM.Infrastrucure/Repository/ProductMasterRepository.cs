using System.Data.Common;
using System.Data.Odbc;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;

namespace AOC_MDM.Infrastrucure.Repository
{
    // Gradian product master (see db/postgres/007_product_master_schema.sql).
    // Every write is a single SQL statement, so the product and its audit row change together.
    public class ProductMasterRepository : IProductMasterRepository
    {
        private readonly OdbcConnection _connection;
        private readonly string _schema;

        // Editable columns: (column, property, SQL type). Same order everywhere they are written.
        private static readonly (string Column, string Property, string SqlType)[] ValueColumns =
        {
            ("product_name", nameof(ProductMasterValues.ProductName), "varchar"),
            ("product_code", nameof(ProductMasterValues.ProductCode), "varchar"),
            ("siop_model", nameof(ProductMasterValues.SiopModel), "varchar"),
            ("siop_tier", nameof(ProductMasterValues.SiopTier), "varchar"),
            ("sections_per_unit", nameof(ProductMasterValues.SectionsPerUnit), "integer"),
            ("sale_price_per_unit", nameof(ProductMasterValues.SalePricePerUnit), "numeric"),
            ("sale_price_per_hour", nameof(ProductMasterValues.SalePricePerHour), "numeric"),
            ("default_supply_source", nameof(ProductMasterValues.DefaultSupplySource), "varchar"),
            ("est_fab_hours", nameof(ProductMasterValues.EstFabHours), "numeric"),
            ("est_assembly_hours", nameof(ProductMasterValues.EstAssemblyHours), "numeric"),
            ("fab_lbr_hrs_pct", nameof(ProductMasterValues.FabLbrHrsPct), "numeric"),
            ("assmb_lbr_hrs_pct", nameof(ProductMasterValues.AssmbLbrHrsPct), "numeric"),
            ("qty_fans", nameof(ProductMasterValues.QtyFans), "integer"),
            ("qty_coils", nameof(ProductMasterValues.QtyCoils), "integer"),
            ("qty_panels", nameof(ProductMasterValues.QtyPanels), "integer"),
            ("standard_unit_multiplier", nameof(ProductMasterValues.StandardUnitMultiplier), "numeric"),
            ("status", nameof(ProductMasterValues.Status), "varchar"),
            ("capacity_group", nameof(ProductMasterValues.CapacityGroup), "varchar"),
        };

        private static readonly string ValueColumnList = string.Join(", ", ValueColumns.Select(c => c.Column));
        private static readonly string ValuePlaceholders = string.Join(", ", ValueColumns.Select(c => $"?::{c.SqlType}"));
        private static readonly string ValueAssignments = string.Join(", ", ValueColumns.Select(c => $"{c.Column} = ?::{c.SqlType}"));

        private const string ProductColumns =
            "product_id, node_id, node_path, product_family, product_type, model, size, product_name, product_code, " +
            "siop_model, siop_tier, sections_per_unit, sale_price_per_unit, sale_price_per_hour, default_supply_source, " +
            "est_fab_hours, est_assembly_hours, fab_lbr_hrs_pct, assmb_lbr_hrs_pct, qty_fans, qty_coils, qty_panels, " +
            "standard_unit_multiplier, status, capacity_group, source_row, createdby, createddate, " +
            "updatedby, updateddate, updatestatus, lastmodifiedbyrole, remarks";

        public ProductMasterRepository(OdbcConnection connection, string catalog)
        {
            _connection = connection;
            _schema = $"{catalog}.mdm";
        }

        // ---------------------------------------------------------------- reads

        public async Task<IReadOnlyList<ProductHierarchyNode>> GetHierarchyAsync()
        {
            var sql =
                "SELECT h.node_id, h.parent_node_id, h.level_no, l.level_name, h.node_name, h.node_path, " +
                "       (SELECT count(*) FROM " + _schema + ".product_master p WHERE p.node_id = h.node_id AND p.isactive) " +
                $"  FROM {_schema}.product_hierarchy h JOIN {_schema}.product_hierarchy_level l ON l.level_no = h.level_no " +
                " WHERE h.isactive ORDER BY h.level_no, lower(h.node_name)";
            return await QueryAsync(sql, r => new ProductHierarchyNode
            {
                NodeId = Convert.ToInt64(r.GetValue(0)),
                ParentNodeId = r.IsDBNull(1) ? null : Convert.ToInt64(r.GetValue(1)),
                LevelNo = Convert.ToInt32(r.GetValue(2)),
                LevelName = r.GetString(3),
                NodeName = r.GetString(4),
                NodePath = r.GetString(5),
                ProductCount = Convert.ToInt32(r.GetValue(6)),
            });
        }

        public Task<IReadOnlyList<ProductMasterRecord>> GetProductsAsync() =>
            QueryAsync($"SELECT {ProductColumns} FROM {_schema}.vw_product_master WHERE isactive ORDER BY lower(product_name)", MapProduct);

        public async Task<ProductMasterRecord?> GetProductAsync(long productId) =>
            (await QueryAsync($"SELECT {ProductColumns} FROM {_schema}.vw_product_master WHERE product_id = ?", MapProduct, productId))
                .FirstOrDefault();

        public Task<IReadOnlyList<ProductMasterField>> GetFieldsAsync() =>
            QueryAsync($"SELECT field_order, source_field, column_name, description FROM {_schema}.product_master_field ORDER BY field_order",
                r => new ProductMasterField
                {
                    FieldOrder = Convert.ToInt32(r.GetValue(0)),
                    SourceField = r.GetString(1),
                    ColumnName = r.GetString(2),
                    Description = Str(r, 3),
                });

        public async Task<bool> NameExistsAsync(string productName, long productId) =>
            (await QueryAsync($"SELECT 1 FROM {_schema}.product_master WHERE lower(product_name) = lower(?) AND product_id <> ?",
                r => 1, productName.Trim(), productId)).Count > 0;

        public Task<IReadOnlyList<ProductMasterAudit>> GetAuditsAsync(string? approvalStatus = null, long? productId = null)
        {
            var where = new List<string>();
            var args = new List<object>();
            if (approvalStatus != null) { where.Add("a.approvalstatus = ?"); args.Add(approvalStatus); }
            if (productId != null) { where.Add("a.product_id = ?"); args.Add(productId.Value); }
            var filter = where.Count == 0 ? "" : " WHERE " + string.Join(" AND ", where);
            return QueryAsync(AuditSelect + filter + " ORDER BY a.changetimestamp DESC, a.auditid DESC", MapAudit, args.ToArray());
        }

        public async Task<ProductMasterAudit?> GetAuditAsync(long auditId) =>
            (await QueryAsync(AuditSelect + " WHERE a.auditid = ?", MapAudit, auditId)).FirstOrDefault();

        private string AuditSelect =>
            $"SELECT a.auditid, a.product_id, h.node_path, {string.Join(", ", ValueColumns.Select(c => "a." + c.Column))}, " +
            "       a.changetype, a.approvalstatus, a.makerid, a.checkerid, a.changetimestamp, a.approvaltimestamp, " +
            "       a.lastmodifiedbyrole, a.remarks, a.checker_remarks " +
            $"  FROM {_schema}.product_master_audit a " +
            $"  JOIN {_schema}.product_master p ON p.product_id = a.product_id " +
            $"  JOIN {_schema}.product_hierarchy h ON h.node_id = p.node_id";

        // ---------------------------------------------------------------- writes

        public async Task UpdateDirectlyAsync(long productId, ProductMasterValues values, string userName, string role, string? remarks)
        {
            var sql =
                $"WITH upd AS (UPDATE {_schema}.product_master SET {ValueAssignments}, " +
                "     updatestatus = 'UpdateDirectly', lastmodifiedbyrole = ?, updatedby = ?, updateddate = now(), remarks = ? " +
                "   WHERE product_id = ? RETURNING product_id) " +
                $"INSERT INTO {_schema}.product_master_audit (product_id, {ValueColumnList}, changetype, approvalstatus, " +
                "       makerid, checkerid, changetimestamp, approvaltimestamp, lastmodifiedbyrole, remarks) " +
                $"SELECT product_id, {ValuePlaceholders}, 'Update', 'UpdateDirectly', ?, ?, now(), now(), ?, ? FROM upd";
            using var cmd = Command(sql);
            AddValues(cmd, values);
            Add(cmd, role); Add(cmd, userName); Add(cmd, remarks); Add(cmd, productId);
            AddValues(cmd, values);
            Add(cmd, userName); Add(cmd, userName); Add(cmd, role); Add(cmd, remarks);
            await ExecuteAsync(cmd);
        }

        public async Task<long> SubmitForApprovalAsync(long productId, ProductMasterValues values, string makerId, string role, string? remarks)
        {
            var sql =
                $"WITH ins AS (INSERT INTO {_schema}.product_master_audit (product_id, {ValueColumnList}, changetype, approvalstatus, " +
                "       makerid, changetimestamp, lastmodifiedbyrole, remarks) " +
                $"   VALUES (?, {ValuePlaceholders}, 'Update', 'PendingApproval', ?, now(), ?, ?) RETURNING auditid, product_id) " +
                $"UPDATE {_schema}.product_master p SET updatestatus = 'PendingApproval', lastmodifiedbyrole = ?, " +
                "       updatedby = ?, updateddate = now(), remarks = ? " +
                "  FROM ins WHERE p.product_id = ins.product_id RETURNING ins.auditid";
            using var cmd = Command(sql);
            Add(cmd, productId);
            AddValues(cmd, values);
            Add(cmd, makerId); Add(cmd, role); Add(cmd, remarks);
            Add(cmd, role); Add(cmd, makerId); Add(cmd, remarks);
            try
            {
                return Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
            catch (OdbcException ex) when (IsUniqueViolation(ex))
            {
                throw new InvalidOperationException("This product already has a change waiting for approval.", ex);
            }
        }

        public async Task<bool> ApproveAsync(long auditId, ProductMasterValues values, string checkerId, string role, string? remarks)
        {
            var sql =
                $"WITH a AS (UPDATE {_schema}.product_master_audit SET approvalstatus = 'Approved', checkerid = ?, " +
                "     approvaltimestamp = now(), checker_remarks = ? " +
                "   WHERE auditid = ? AND approvalstatus = 'PendingApproval' RETURNING product_id) " +
                $"UPDATE {_schema}.product_master p SET {ValueAssignments}, " +
                "       updatestatus = 'Approved', lastmodifiedbyrole = ?, updatedby = ?, updateddate = now(), remarks = ? " +
                "  FROM a WHERE p.product_id = a.product_id";
            using var cmd = Command(sql);
            Add(cmd, checkerId); Add(cmd, remarks); Add(cmd, auditId);
            AddValues(cmd, values);
            Add(cmd, role); Add(cmd, checkerId); Add(cmd, remarks);
            return await ExecuteAsync(cmd) > 0;
        }

        public async Task<bool> RejectAsync(long auditId, string checkerId, string role, string remarks)
        {
            var sql =
                $"WITH a AS (UPDATE {_schema}.product_master_audit SET approvalstatus = 'Rejected', checkerid = ?, " +
                "     approvaltimestamp = now(), checker_remarks = ? " +
                "   WHERE auditid = ? AND approvalstatus = 'PendingApproval' RETURNING product_id) " +
                $"UPDATE {_schema}.product_master p SET updatestatus = 'Rejected', lastmodifiedbyrole = ?, " +
                "       updatedby = ?, updateddate = now(), remarks = ? " +
                "  FROM a WHERE p.product_id = a.product_id";
            using var cmd = Command(sql);
            Add(cmd, checkerId); Add(cmd, remarks); Add(cmd, auditId);
            Add(cmd, role); Add(cmd, checkerId); Add(cmd, remarks);
            return await ExecuteAsync(cmd) > 0;
        }

        // ---------------------------------------------------------------- helpers

        private OdbcCommand Command(string sql) => new(sql, _connection);

        private static void Add(OdbcCommand cmd, object? value) =>
            cmd.Parameters.Add(new OdbcParameter { Value = value ?? DBNull.Value });

        private static void AddValues(OdbcCommand cmd, ProductMasterValues values)
        {
            foreach (var c in ValueColumns)
            {
                var v = typeof(ProductMasterValues).GetProperty(c.Property)!.GetValue(values);
                if (v is string s) v = string.IsNullOrWhiteSpace(s) ? null : s.Trim();
                Add(cmd, v);
            }
        }

        private static async Task<int> ExecuteAsync(OdbcCommand cmd)
        {
            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (OdbcException ex) when (IsUniqueViolation(ex))
            {
                throw new InvalidOperationException("Another product already uses this Product Name.", ex);
            }
        }

        private static bool IsUniqueViolation(OdbcException ex) =>
            ex.Errors.Cast<OdbcError>().Any(e => e.SQLState == "23505");

        private static ProductMasterRecord MapProduct(DbDataReader r) => new()
        {
            ProductId = Convert.ToInt64(r.GetValue(0)),
            NodeId = Convert.ToInt64(r.GetValue(1)),
            NodePath = r.GetString(2),
            ProductFamily = r.GetString(3),
            ProductType = Str(r, 4) ?? "",
            Model = Str(r, 5),
            Size = Str(r, 6),
            ProductName = r.GetString(7),
            ProductCode = Str(r, 8),
            SiopModel = Str(r, 9),
            SiopTier = Str(r, 10),
            SectionsPerUnit = Int(r, 11),
            SalePricePerUnit = Dec(r, 12),
            SalePricePerHour = Dec(r, 13),
            DefaultSupplySource = Str(r, 14),
            EstFabHours = Dec(r, 15),
            EstAssemblyHours = Dec(r, 16),
            FabLbrHrsPct = Dec(r, 17),
            AssmbLbrHrsPct = Dec(r, 18),
            QtyFans = Int(r, 19),
            QtyCoils = Int(r, 20),
            QtyPanels = Int(r, 21),
            StandardUnitMultiplier = Dec(r, 22),
            Status = r.GetString(23),
            CapacityGroup = Str(r, 24),
            SourceRow = Int(r, 25),
            CreatedBy = Str(r, 26),
            CreatedDate = Date(r, 27),
            UpdatedBy = Str(r, 28),
            UpdatedDate = Date(r, 29),
            UpdateStatus = Str(r, 30),
            LastModifiedByRole = Str(r, 31),
            Remarks = Str(r, 32),
        };

        private static ProductMasterAudit MapAudit(DbDataReader r)
        {
            var a = new ProductMasterAudit
            {
                AuditId = Convert.ToInt64(r.GetValue(0)),
                ProductId = Convert.ToInt64(r.GetValue(1)),
                NodePath = r.GetString(2),
            };
            var i = 3;
            foreach (var c in ValueColumns)
            {
                var prop = typeof(ProductMasterValues).GetProperty(c.Property)!;
                object? v = c.SqlType switch
                {
                    "integer" => Int(r, i),
                    "numeric" => Dec(r, i),
                    _ => Str(r, i),
                };
                prop.SetValue(a, v ?? (c.Column is "product_name" or "status" ? "" : null)); // non-nullable strings
                i++;
            }
            a.ChangeType = Str(r, i++) ?? "Update";
            a.ApprovalStatus = Str(r, i++) ?? "";
            a.MakerId = Str(r, i++);
            a.CheckerId = Str(r, i++);
            a.ChangeTimestamp = Date(r, i++) ?? DateTime.MinValue;
            a.ApprovalTimestamp = Date(r, i++);
            a.LastModifiedByRole = Str(r, i++);
            a.Remarks = Str(r, i++);
            a.CheckerRemarks = Str(r, i);
            return a;
        }

        private static string? Str(DbDataReader r, int i) => r.IsDBNull(i) ? null : r.GetValue(i).ToString();
        private static int? Int(DbDataReader r, int i) => r.IsDBNull(i) ? null : Convert.ToInt32(r.GetValue(i));
        private static decimal? Dec(DbDataReader r, int i) => r.IsDBNull(i) ? null : Convert.ToDecimal(r.GetValue(i));
        private static DateTime? Date(DbDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDateTime(i);

        private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, Func<DbDataReader, T> map, params object[] args)
        {
            var list = new List<T>();
            using var cmd = Command(sql);
            foreach (var a in args) Add(cmd, a);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(map(r));
            return list;
        }
    }
}
