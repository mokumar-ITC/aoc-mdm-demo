using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq.Expressions;
using System.Reflection;

namespace AOC_MDM.Infrastrucure.Repository
{

    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected AOCMDMDbContext _context;
        protected DbSet<T> dbSet;
        protected readonly ILogger _logger;

        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly string _catalog;
        private readonly OdbcConnection _connection; // Reused connection

        public GenericRepository(OdbcConnection connection, string tableName, string catalog)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _tableName = tableName;
            _catalog = catalog;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var items = new List<T>();
            using (var command = new OdbcCommand($"SELECT * FROM {_tableName}", _connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = MapReaderToEntity(reader); // Correct usage here
                        items.Add(item);
                    }
                }
            }
            return items;
        }

        public async Task<IEnumerable<T>> SearchAllColumnsAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                throw new ArgumentException("Search text cannot be empty", nameof(searchText));

            var items = new List<T>();

            // Get all column names dynamically
            var columnNames = await GetColumnNamesAsync(_tableName);
            if (columnNames == null || !columnNames.Any())
                throw new InvalidOperationException($"No columns found for table {_tableName}");

            // Build dynamic WHERE condition (search across all columns)
            var whereClauses = new List<string>();
            var parameters = new List<OdbcParameter>();

            int paramIndex = 0;
            foreach (var column in columnNames)
            {
                string paramName = $"@p{paramIndex}";
                whereClauses.Add($"LOWER(CAST({column} AS VARCHAR)) LIKE LOWER(?)");
                parameters.Add(new OdbcParameter(paramName, $"%{searchText.ToLower()}%"));
                paramIndex++;
            }

            string whereClause = string.Join(" OR ", whereClauses);

            // Construct final SQL query
            string query = $"SELECT * FROM {_tableName} WHERE {whereClause}";

            using (var command = new OdbcCommand(query, _connection))
            {
                command.Parameters.AddRange(parameters.ToArray());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = MapReaderToEntity(reader);
                        items.Add(item);
                    }
                }
            }

            return items;
        }




        public async Task<IEnumerable<T>> GetFilteredAsync(string whereClause)
        {
            var items = new List<T>();
            var query = $"SELECT * FROM {_tableName} WHERE {whereClause}";

            using (var command = new OdbcCommand(query, _connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = MapReaderToEntity(reader);
                        items.Add(item);
                    }
                }
            }
            return items;
        }


        public async Task<T> GetSingleFilteredAsync(string whereClause)
        {
            var query = $"SELECT * FROM {_tableName} WHERE {whereClause} LIMIT 1";

            using (var command = new OdbcCommand(query, _connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapReaderToEntity(reader);
                    }
                }
            }

            return default; // Returns null if no matching record is found
        }



        public async Task<PaginatedResult<T>> GetAllPaginatedAsync(
    int pageNumber,
    int pageSize,
    string filterColumn = null,
    string filterValue = null)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                var startRow = (pageNumber - 1) * pageSize + 1;
                var endRow = startRow + pageSize - 1;

                // Get column names dynamically and validate filterColumn
                var columnNames = await GetColumnNamesAsync(_tableName);
                var columns = string.Join(", ", columnNames);

                // Validate and sanitize filterColumn
                if (!string.IsNullOrWhiteSpace(filterColumn) && !columnNames.Contains(filterColumn))
                {
                    throw new ArgumentException($"Invalid column name: {filterColumn}");
                }


                string filterCondition = string.Empty;

                if (!string.IsNullOrWhiteSpace(filterColumn))
                {
                    if (string.Equals(filterValue, "__unmapped__"))
                    {
                        // Unmapped data: no value in the mapping column (e.g. level_3)
                        filterCondition = $@"AND ({filterColumn} IS NULL OR {filterColumn} = '')";
                    }
                    else if (filterColumn == "UpdateStatus" && string.Equals(filterValue, "New"))
                    {
                        // Special handling for UpdatedStatus = 'New'
                        filterCondition = $@"AND {filterColumn} IS NULL";
                    }
                    else if (!string.IsNullOrWhiteSpace(filterValue))
                    {
                        // Handle non-NULL filtering with pattern matching
                        filterCondition = $@"AND {filterColumn} ILIKE '%{filterValue.Replace("'", "''")}%'";
                    }
                }

                // Query to get total count of records
                var countQuery = $@"
SELECT COUNT(*)
FROM {_tableName}
WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE)
{filterCondition}";

                // Construct the query with dynamic columns, filtering, and pagination
                var query = $@"
SELECT {columns}
FROM (
    SELECT {columns}, ROW_NUMBER() OVER (ORDER BY UpdatedDate DESC) AS RowNum
    FROM {_tableName}
    WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE)
    {filterCondition}
) AS Paginated
WHERE RowNum BETWEEN {startRow} AND {endRow}";

                var items = new List<T>();
                int totalCount = 0;

                // Get total count
                using (var countCommand = new OdbcCommand(countQuery, _connection))
                {
                    totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }

                // Get paginated data
                using (var command = new OdbcCommand(query, _connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Map dynamically based on column names
                            var item = MapReaderToEntity(reader, columnNames);
                            items.Add(item);
                        }
                    }
                }

                return new PaginatedResult<T>
                {
                    Items = items,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<PaginatedResult<T>> GetAllPaginatedAsync(
    int pageNumber,
    int pageSize,
    string filterColumn = null,
    string filterValue = null,
    string sortColumn = "UpdatedDate",  // Default sorting column
    bool sortAscending = false)         // Default to descending order
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                var startRow = (pageNumber - 1) * pageSize + 1;
                var endRow = startRow + pageSize - 1;

                // Get column names dynamically and validate filterColumn & sortColumn
                var columnNames = await GetColumnNamesAsync(_tableName);
                var columns = string.Join(", ", columnNames);

                if (!string.IsNullOrWhiteSpace(filterColumn) && !columnNames.Contains(filterColumn))
                {
                    throw new ArgumentException($"Invalid column name: {filterColumn}");
                }

                if (!string.IsNullOrWhiteSpace(sortColumn) && !columnNames.Contains(sortColumn))
                {
                    throw new ArgumentException($"Invalid sort column: {sortColumn}");
                }

                string filterCondition = string.Empty;

                if (!string.IsNullOrWhiteSpace(filterColumn))
                {
                    if (string.Equals(filterValue, "__unmapped__"))
                    {
                        // Unmapped data: no value in the mapping column (e.g. level_3)
                        filterCondition = $@"AND ({filterColumn} IS NULL OR {filterColumn} = '')";
                    }
                    else if (filterColumn == "UpdateStatus" && string.Equals(filterValue, "New"))
                    {
                        filterCondition = $@"AND {filterColumn} IS NULL";
                    }
                    else if (!string.IsNullOrWhiteSpace(filterValue))
                    {
                        filterCondition = $@"AND {filterColumn} ILIKE '%{filterValue.Replace("'", "''")}%'";
                    }
                }

                // Determine sorting direction
                string sortDirection = sortAscending ? "ASC" : "DESC";

                // Query to get total count of records
                var countQuery = $@"
SELECT COUNT(*)
FROM {_tableName}
WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE)
{filterCondition}";

                // Construct the query with dynamic columns, filtering, pagination, and sorting
                var query = $@"
SELECT {columns}
FROM (
    SELECT {columns}, ROW_NUMBER() OVER (ORDER BY {sortColumn} {sortDirection}) AS RowNum
    FROM {_tableName}
    WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE)
    {filterCondition}
) AS Paginated
WHERE RowNum BETWEEN {startRow} AND {endRow}";

                var items = new List<T>();
                int totalCount = 0;

                // Get total count
                using (var countCommand = new OdbcCommand(countQuery, _connection))
                {
                    totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }

                // Get paginated data
                using (var command = new OdbcCommand(query, _connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = MapReaderToEntity(reader, columnNames);
                            items.Add(item);
                        }
                    }
                }

                return new PaginatedResult<T>
                {
                    Items = items,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }



        public async Task<PaginatedResult<T>> GetDataPlusGlobalSearchAsync(
    int pageNumber,
    int pageSize,
    string filterColumn = null,
    string filterValue = null,
    string searchText = null)  // New input parameter for global search
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                var startRow = (pageNumber - 1) * pageSize + 1;
                var endRow = startRow + pageSize - 1;

                // Get column names dynamically and validate filterColumn
                var columnNames = await GetColumnNamesAsync(_tableName);
                var columns = string.Join(", ", columnNames);

                // Validate and sanitize filterColumn
                if (!string.IsNullOrWhiteSpace(filterColumn) && !columnNames.Contains(filterColumn))
                {
                    throw new ArgumentException($"Invalid column name: {filterColumn}");
                }

                var filterConditions = new List<string>();

                // Apply existing filtering condition (if filterColumn and filterValue are provided)
                if (!string.IsNullOrWhiteSpace(filterColumn))
                {
                    if (string.Equals(filterValue, "__unmapped__"))
                    {
                        // Unmapped data: no value in the mapping column (e.g. level_3)
                        filterConditions.Add($"({filterColumn} IS NULL OR {filterColumn} = '')");
                    }
                    else if (filterColumn == "UpdateStatus" && string.Equals(filterValue, "New"))
                    {
                        filterConditions.Add($"{filterColumn} IS NULL");
                    }
                    else if (!string.IsNullOrWhiteSpace(filterValue))
                    {
                        filterConditions.Add($"{filterColumn} ILIKE '%{filterValue.Replace("'", "''")}%'");
                    }
                }

                // Apply global search across all columns (case-insensitive)
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchConditions = columnNames
                        .Select(column => $"LOWER(CAST({column} AS VARCHAR)) LIKE LOWER('%{searchText.Replace("'", "''")}%')")
                        .ToList();
                    filterConditions.Add($"({string.Join(" OR ", searchConditions)})");
                }

                // Build the WHERE clause
                string whereClause = filterConditions.Any()
                    ? "WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE) AND " + string.Join(" AND ", filterConditions)
                    : "WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE)";

                // Query to get total count of records
                var countQuery = $@"
SELECT COUNT(*)
FROM {_tableName}
{whereClause}";

                // Construct the query with dynamic columns, filtering, and pagination
                var query = $@"
SELECT {columns}
FROM (
    SELECT {columns}, ROW_NUMBER() OVER (ORDER BY UpdatedDate DESC) AS RowNum
    FROM {_tableName}
    {whereClause}
) AS Paginated
WHERE RowNum BETWEEN {startRow} AND {endRow}";

                var items = new List<T>();
                int totalCount = 0;

                // Get total count
                using (var countCommand = new OdbcCommand(countQuery, _connection))
                {
                    totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }

                // Get paginated data
                using (var command = new OdbcCommand(query, _connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = MapReaderToEntity(reader, columnNames);
                            items.Add(item);
                        }
                    }
                }

                return new PaginatedResult<T>
                {
                    Items = items,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }


        public async Task<PaginatedResult<T>> GetDataPlusGlobalSearchAsync(
    int pageNumber,
    int pageSize,
    string filterColumn = null,
    string filterValue = null,
    string searchText = null,
    string sortColumn = "UpdatedDate",  // Default sorting column
    bool sortAscending = false)         // Default to descending order
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                var startRow = (pageNumber - 1) * pageSize + 1;
                var endRow = startRow + pageSize - 1;

                // Get column names dynamically and validate filterColumn & sortColumn
                var columnNames = await GetColumnNamesAsync(_tableName);
                var columns = string.Join(", ", columnNames);

                if (!string.IsNullOrWhiteSpace(filterColumn) && !columnNames.Contains(filterColumn))
                {
                    throw new ArgumentException($"Invalid column name: {filterColumn}");
                }

                if (!string.IsNullOrWhiteSpace(sortColumn) && !columnNames.Contains(sortColumn))
                {
                    throw new ArgumentException($"Invalid sort column: {sortColumn}");
                }

                var filterConditions = new List<string>();

                // Apply existing filtering condition (if filterColumn and filterValue are provided)
                if (!string.IsNullOrWhiteSpace(filterColumn))
                {
                    if (string.Equals(filterValue, "__unmapped__"))
                    {
                        // Unmapped data: no value in the mapping column (e.g. level_3)
                        filterConditions.Add($"({filterColumn} IS NULL OR {filterColumn} = '')");
                    }
                    else if (filterColumn == "UpdateStatus" && string.Equals(filterValue, "New"))
                    {
                        filterConditions.Add($"{filterColumn} IS NULL");
                    }
                    else if (!string.IsNullOrWhiteSpace(filterValue))
                    {
                        filterConditions.Add($"{filterColumn} ILIKE '%{filterValue.Replace("'", "''")}%'");
                    }
                }

                // Apply global search across all columns (case-insensitive)
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchConditions = columnNames
                        .Select(column => $"LOWER(CAST({column} AS VARCHAR)) LIKE LOWER('%{searchText.Replace("'", "''")}%')")
                        .ToList();
                    filterConditions.Add($"({string.Join(" OR ", searchConditions)})");
                }

                // Build the WHERE clause
                string whereClause = filterConditions.Any()
                    ? "WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE) AND " + string.Join(" AND ", filterConditions)
                    : "WHERE (IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE)";

                // Determine sorting direction
                string sortDirection = sortAscending ? "ASC" : "DESC";

                // Query to get total count of records
                var countQuery = $@"
SELECT COUNT(*)
FROM {_tableName}
{whereClause}";

                // Construct the query with dynamic columns, filtering, pagination, and sorting
                var query = $@"
SELECT {columns}
FROM (
    SELECT {columns}, ROW_NUMBER() OVER (ORDER BY {sortColumn} {sortDirection}) AS RowNum
    FROM {_tableName}
    {whereClause}
) AS Paginated
WHERE RowNum BETWEEN {startRow} AND {endRow}";

                var items = new List<T>();
                int totalCount = 0;

                // Get total count
                using (var countCommand = new OdbcCommand(countQuery, _connection))
                {
                    totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }

                // Get paginated data
                using (var command = new OdbcCommand(query, _connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = MapReaderToEntity(reader, columnNames);
                            items.Add(item);
                        }
                    }
                }

                return new PaginatedResult<T>
                {
                    Items = items,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }


        public async Task<IEnumerable<T>> GetDataPlusGlobalSearchAsync(
    string filterColumn = null,
    string filterValue = null,
    string searchText = null)  // New input parameter for global search
        {
            try
            {
                // Get column names dynamically (supports tables & views)
                var columnNames = await GetColumnNamesForVIEWOrTableAsync(_tableName);
                var columns = string.Join(", ", columnNames);

                // Validate and sanitize filterColumn
                if (!string.IsNullOrWhiteSpace(filterColumn) && !columnNames.Contains(filterColumn))
                {
                    throw new ArgumentException($"Invalid column name: {filterColumn}");
                }

                var filterConditions = new List<string>();

                // Apply column-based filtering (if filterColumn and filterValue are provided)
                if (!string.IsNullOrWhiteSpace(filterColumn))
                {
                    if (string.Equals(filterValue, "__unmapped__"))
                    {
                        // Unmapped data: no value in the mapping column (e.g. level_3)
                        filterConditions.Add($"({filterColumn} IS NULL OR {filterColumn} = '')");
                    }
                    else if (filterColumn == "UpdateStatus" && string.Equals(filterValue, "New"))
                    {
                        filterConditions.Add($"{filterColumn} IS NULL");
                    }
                    else if (!string.IsNullOrWhiteSpace(filterValue))
                    {
                        filterConditions.Add($"{filterColumn} ILIKE '%{filterValue.Replace("'", "''")}%'");
                    }
                }

                // Apply global search across all columns (case-insensitive)
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchConditions = columnNames
                        .Select(column => $"LOWER(CAST({column} AS VARCHAR)) LIKE LOWER('%{searchText.Replace("'", "''")}%')")
                        .ToList();
                    filterConditions.Add($"({string.Join(" OR ", searchConditions)})");
                }

                // Build the WHERE clause dynamically
                string whereClause = filterConditions.Any()
                    ? "WHERE " + string.Join(" AND ", filterConditions)
                    : "";

                // Construct the query with dynamic columns and filtering
                var query = $@"
SELECT {columns}
FROM {_tableName}
{whereClause}";

                var items = new List<T>();

                // Execute query and fetch data
                using (var command = new OdbcCommand(query, _connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = MapReaderToEntity(reader, columnNames);
                            items.Add(item);
                        }
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }



        public async Task<IEnumerable<T>> GetAllPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var startRow = (pageNumber - 1) * pageSize + 1;
            var endRow = startRow + pageSize - 1;

            // Get column names dynamically
            var columnNames = await GetColumnNamesAsync(_tableName);
            var columns = string.Join(", ", columnNames); // Build column list dynamically

            // Construct the query with dynamic columns and pagination
            var query = $@"
    SELECT {columns}
    FROM (
        SELECT {columns}, ROW_NUMBER() OVER (ORDER BY UpdatedDate DESC) AS RowNum
        FROM {_tableName}
        WHERE IsActive = TRUE OR IsActive IS NULL OR IsActive = FALSE
    ) AS Paginated
    WHERE RowNum BETWEEN {startRow} AND {endRow}";  // Directly inject the row numbers

            var items = new List<T>();

            using (var command = new OdbcCommand(query, _connection))
            {
                // Execute the query without parameterized row numbers
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Map dynamically based on column names
                        var item = MapReaderToEntity(reader, columnNames);
                        items.Add(item);
                    }
                }
            }

            return items;
        }


        private T MapReaderToEntity(IDataReader reader, IEnumerable<string> columnNames)
        {
            var entity = Activator.CreateInstance<T>();

            foreach (var column in columnNames)
            {
                // Find the property that is mapped to the column using the ColumnAttribute
                var property = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p =>
                        p.GetCustomAttributes(typeof(ColumnAttribute), false)
                         .Cast<ColumnAttribute>()
                         .Any(attr => attr.Name.Equals(column, StringComparison.OrdinalIgnoreCase)) ||
                        p.Name.Equals(column, StringComparison.OrdinalIgnoreCase));

                if (property != null)
                {
                    // Check if the column value is DBNull (null in database) and handle nullable properties
                    if (!reader.IsDBNull(reader.GetOrdinal(column)))
                    {
                        var value = reader[column];

                        // Check if the property is nullable
                        if (Nullable.GetUnderlyingType(property.PropertyType) != null)
                        {
                            // Convert to Nullable<T> type if it's a nullable type
                            var nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                            property.SetValue(entity, value == DBNull.Value ? null : Convert.ChangeType(value, nullableType));
                        }
                        else
                        {
                            // If it's not nullable, just set the value
                            property.SetValue(entity, Convert.ChangeType(value, property.PropertyType));
                        }
                    }
                }
            }

            return entity;
        }



        private async Task<IEnumerable<string>> GetColumnNamesAsync(string tableName)
        {
            return await GetColumnNamesForVIEWOrTableAsync(tableName);
        }

        // PostgreSQL: read column metadata from information_schema (replaces Databricks DESCRIBE / SHOW COLUMNS).
        // PostgreSQL folds unquoted names to lower case, so names are returned with the entity's [Column]
        // spelling (e.g. "UpdateStatus") to keep the case-sensitive filter/sort column checks working.
        private async Task<List<string>> GetColumnNamesForVIEWOrTableAsync(string tableName)
        {
            var parts = tableName.Split('.');
            var schema = parts.Length >= 2 ? parts[^2] : "public";
            var table = parts[^1];

            var entityColumns = typeof(T).GetProperties()
                .Select(p => (p.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute)?.Name ?? p.Name)
                .ToList();

            var columnNames = new List<string>();
            const string query = "SELECT column_name FROM information_schema.columns WHERE table_schema = ? AND table_name = ? ORDER BY ordinal_position";

            using (var command = new OdbcCommand(query, _connection))
            {
                command.Parameters.Add(new OdbcParameter("@schema", schema.ToLowerInvariant()));
                command.Parameters.Add(new OdbcParameter("@table", table.ToLowerInvariant()));
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var dbName = reader.GetString(0);
                        columnNames.Add(entityColumns.FirstOrDefault(c => c.Equals(dbName, StringComparison.OrdinalIgnoreCase)) ?? dbName);
                    }
                }
            }

            if (columnNames.Count == 0)
                throw new InvalidOperationException($"Table or view '{tableName}' not found or has no columns.");

            return columnNames;
        }


        public async Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string tableName, string columnName, string filter = null)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.");


            // Construct query to fetch distinct column values, excluding nulls
            var query = $@"SELECT DISTINCT {columnName} FROM {tableName} WHERE {columnName} IS NOT NULL";

            // Add optional filter
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query += $" AND {filter}";
            }

            using (var command = new OdbcCommand(query, _connection))
            {
                var result = new List<object>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(reader[0]);
                    }
                }
                return result;
            }
        }

        public async Task<DataTable> GetRecsWithAffectedAttributesAsync(
    string tableName,
    string columnName,
    string columnValue)
        {
            var resultTable = new DataTable();

            try
            {
                // Split columnValue into individual values
                var values = columnValue
                    .Split(',')
                    .Select(v => v.Trim().Replace("'", "''"))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                if (!values.Any())
                {
                    throw new ArgumentException("Column value must contain at least one valid value.");
                }

                // Determine whether to use '=' or 'IN' clause
                string condition;
                if (values.Count == 1)
                {
                    condition = $"{columnName} = '{values.First()}'";
                }
                else
                {
                    condition = $"{columnName} IN ({string.Join(", ", values.Select(v => $"'{v}'"))})";
                }

                // Query for vendor_reference_attributes
                string vendorQuery = $@"
            SELECT COUNT(*) AS TotalVendorRecords
            FROM {_catalog}.mdm.vendor_reference_attributes
            WHERE {condition}";

                // Query for item_reference_attributes
                string itemQuery = $@"
            SELECT COUNT(*) AS TotalItemRecords
            FROM {_catalog}.mdm.item_reference_attributes
            WHERE {condition}";

                // Query for the main table (tableName)
                string mainTableQuery = $@"
SELECT * 
FROM {tableName}
WHERE {columnName} = '{columnValue.Replace("'", "''")}'";


                int totalVendorRecords = 0;
                int totalItemRecords = 0;

                // Execute queries for total records
                using (var vendorCommand = new OdbcCommand(vendorQuery, _connection))
                {
                    totalVendorRecords = Convert.ToInt32(await vendorCommand.ExecuteScalarAsync());
                }

                using (var itemCommand = new OdbcCommand(itemQuery, _connection))
                {
                    totalItemRecords = Convert.ToInt32(await itemCommand.ExecuteScalarAsync());
                }

                // Execute the main table query
                using (var mainTableCommand = new OdbcCommand(mainTableQuery, _connection))
                {
                    using (var reader = await mainTableCommand.ExecuteReaderAsync())
                    {
                        resultTable.Load(reader);
                    }
                }

                // Add new columns to the result table
                if (!resultTable.Columns.Contains("Total_Effected_Recs_of_Vendor"))
                {
                    resultTable.Columns.Add("Total_Effected_Recs_of_Vendor", typeof(int));
                }

                if (!resultTable.Columns.Contains("Total_Effected_Recs_of_Item"))
                {
                    resultTable.Columns.Add("Total_Effected_Recs_of_Item", typeof(int));
                }

                // Populate the additional columns for all rows
                foreach (DataRow row in resultTable.Rows)
                {
                    row["Total_Effected_Recs_of_Vendor"] = totalVendorRecords;
                    row["Total_Effected_Recs_of_Item"] = totalItemRecords;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error querying Databricks tables", ex);
            }

            return resultTable;
        }


        public async Task AddAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Get properties of the entity
            var properties = typeof(T).GetProperties();

            // Get column mappings
            var columnMappings = properties.Select(p =>
            {
                // Check if the property has a [Key] attribute
                var isKey = p.GetCustomAttributes(typeof(KeyAttribute), false).Any();

                // Get column name from attributes or property name
                var columnAttr = p.GetCustomAttributes(typeof(ColumnAttribute), false)
                                  .FirstOrDefault() as ColumnAttribute;
                var columnName = columnAttr?.Name ?? p.Name;

                // Skip identity keys (numeric [Key], e.g. AuditId); business keys such as vendor_id must be inserted
                if (isKey && (p.PropertyType == typeof(long) || p.PropertyType == typeof(int))) return null;

                var value = p.GetValue(entity);

                // Handle DateTime fields
                if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                {
                    if (value == null)
                    {
                        return new
                        {
                            ColumnName = columnName,
                            Value = "CAST(NULL AS TIMESTAMP)" // NULL for TIMESTAMP fields
                        };
                    }
                    else
                    {
                        var dateTimeValue = (DateTime)value;
                        return new
                        {
                            ColumnName = columnName,
                            Value = $"'{dateTimeValue:yyyy-MM-dd HH:mm:ss.fff}'" // Format DateTime for SQL
                        };
                    }
                }

                // Handle NULL values for other types
                if (value == null || value == DBNull.Value)
                {
                    return new
                    {
                        ColumnName = columnName,
                        Value = "NULL" // Regular NULL for non-TIMESTAMP fields
                    };
                }

                // Handle non-NULL values
                return new
                {
                    ColumnName = columnName,
                    Value = $"'{value}'" // Safely add single quotes for other types
                };
            })
            .Where(m => m != null) // Exclude skipped columns
            .ToList();

            // Construct SQL query
            var columnNames = string.Join(", ", columnMappings.Select(m => m.ColumnName));
            var values = string.Join(", ", columnMappings.Select(m => m.Value));

            var query = $"INSERT INTO {_tableName} ({columnNames}) VALUES ({values})";

            try
            {
                using (var command = new OdbcCommand(query, _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                // Log or rethrow exception as needed
                throw new InvalidOperationException($"Error inserting data into {_tableName}: {ex.Message}", ex);
            }
        }

        public async Task UpdateAsync(T entity, object key, string primaryColumnName)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var properties = GetCachedProperties(typeof(T));

            var setClauses = new List<string>();
            var parameters = new List<OdbcParameter>();

            foreach (var property in properties)
            {
                if (property.IsKey) continue;

                var columnName = property.ColumnName;
                var value = property.PropertyInfo.GetValue(entity);

                // Handle DateTime fields (replace DateTime.MinValue with NULL)
                if (property.PropertyInfo.PropertyType == typeof(DateTime) || property.PropertyInfo.PropertyType == typeof(DateTime?))
                {
                    if (value == null || (DateTime)value == DateTime.MinValue)
                    {
                        setClauses.Add($"{columnName} = NULL");
                    }
                    else
                    {
                        var formattedDate = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        setClauses.Add($"{columnName} = ?");
                        parameters.Add(new OdbcParameter($"@{columnName}", formattedDate));
                    }
                    continue;
                }

                // Handle null values
                if (value == null || value == DBNull.Value)
                {
                    setClauses.Add($"{columnName} = NULL");
                    continue;
                }

                // Handle other fields
                setClauses.Add($"{columnName} = ?");
                parameters.Add(new OdbcParameter($"@{columnName}", value));
            }

            var setClause = string.Join(", ", setClauses);
            var query = $"UPDATE {_tableName} SET {setClause} WHERE {primaryColumnName} = ?";

            parameters.Add(new OdbcParameter("@PrimaryKey", key));

            try
            {
                using (var command = new OdbcCommand(query, _connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"Record with {primaryColumnName} = '{key}' not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating data in {_tableName}: {ex.Message}", ex);
            }
        }

        public async Task UpdateAsync(T entity, object key, string primaryColumnName, string sourceColumnName = null, string sourceValue = null, string vendorColumnName = null, string vendorValue = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var properties = GetCachedProperties(typeof(T));

            var setClauses = new List<string>();
            var parameters = new List<OdbcParameter>();

            foreach (var property in properties)
            {
                if (property.IsKey) continue;

                var columnName = property.ColumnName;
                var value = property.PropertyInfo.GetValue(entity);

                // Handle DateTime fields (replace DateTime.MinValue with NULL)
                if (property.PropertyInfo.PropertyType == typeof(DateTime) || property.PropertyInfo.PropertyType == typeof(DateTime?))
                {
                    if (value == null || (DateTime)value == DateTime.MinValue)
                    {
                        setClauses.Add($"{columnName} = NULL");
                    }
                    else
                    {
                        var formattedDate = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        setClauses.Add($"{columnName} = ?");
                        parameters.Add(new OdbcParameter($"@{columnName}", formattedDate));
                    }
                    continue;
                }

                // Handle null values
                if (value == null || value == DBNull.Value)
                {
                    setClauses.Add($"{columnName} = NULL");
                    continue;
                }

                // Handle other fields
                setClauses.Add($"{columnName} = ?");
                parameters.Add(new OdbcParameter($"@{columnName}", value));
            }

            var setClause = string.Join(", ", setClauses);

            // Build WHERE clause with optional source and vendor column checks
            var whereClause = $"{primaryColumnName} = ?";
            parameters.Add(new OdbcParameter("@PrimaryKey", key));

            if (!string.IsNullOrEmpty(sourceColumnName) && !string.IsNullOrEmpty(sourceValue))
            {
                whereClause += $" AND {sourceColumnName} = ?";
                parameters.Add(new OdbcParameter($"@{sourceColumnName}", sourceValue));
            }

            if (!string.IsNullOrEmpty(vendorColumnName) && !string.IsNullOrEmpty(vendorValue))
            {
                whereClause += $" AND {vendorColumnName} = ?";
                parameters.Add(new OdbcParameter($"@{vendorColumnName}", vendorValue));
            }

            var query = $"UPDATE {_tableName} SET {setClause} WHERE {whereClause}";

            try
            {
                using (var command = new OdbcCommand(query, _connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"No record found with {primaryColumnName} = '{key}'" +
                                                            $"{(!string.IsNullOrEmpty(sourceColumnName) ? $" and {sourceColumnName} = '{sourceValue}'" : string.Empty)}" +
                                                            $"{(!string.IsNullOrEmpty(vendorColumnName) ? $" and {vendorColumnName} = '{vendorValue}'" : string.Empty)}.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating data in {_tableName}: {ex.Message}", ex);
            }
        }




        #region Helper Methods for Property Reflection Cache
        // Cache entity property metadata to improve performance
        private static readonly ConcurrentDictionary<Type, List<PropertyMapping>> _propertyCache = new();

        private List<PropertyMapping> GetCachedProperties(Type type)
        {
            if (_propertyCache.TryGetValue(type, out var cachedProperties))
                return cachedProperties;

            // Process metadata for caching
            var properties = type.GetProperties()
                                 .Select(p =>
                                 {
                                     var columnAttr = p.GetCustomAttribute<ColumnAttribute>();
                                     var isKey = p.GetCustomAttribute<KeyAttribute>() != null;
                                     return new PropertyMapping
                                     {
                                         ColumnName = columnAttr?.Name ?? p.Name,
                                         PropertyInfo = p,
                                         IsKey = isKey
                                     };
                                 })
                                 .Where(p => !p.IsKey) // Exclude keys
                                 .ToList();

            _propertyCache[type] = properties;
            return properties;
        }

        private List<PropertyMapping> GetCachedPropertiesIncludingPK(Type type)
        {
            if (_propertyCache.TryGetValue(type, out var cachedProperties))
                return cachedProperties;

            // Process metadata for caching
            var properties = type.GetProperties()
                                 .Select(p =>
                                 {
                                     var columnAttr = p.GetCustomAttribute<ColumnAttribute>();
                                     var isKey = p.GetCustomAttribute<KeyAttribute>() != null;
                                     return new PropertyMapping
                                     {
                                         ColumnName = columnAttr?.Name ?? p.Name,
                                         PropertyInfo = p,
                                         IsKey = isKey
                                     };
                                 })
                                 //.Where(p => !p.IsKey) // Exclude keys
                                 .ToList();

            _propertyCache[type] = properties;
            return properties;
        }

        private class PropertyMapping
        {
            public string ColumnName { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public bool IsKey { get; set; }
        }
        #endregion


        private static readonly ConcurrentDictionary<Type, (string Query, List<PropertyInfo> Properties)> EntityCache
    = new ConcurrentDictionary<Type, (string Query, List<PropertyInfo> Properties)>();

        public async Task AddAsync(T entity, string notworkin)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var (queryTemplate, properties) = GetCachedEntityData<T>(_tableName);

            // Manually substitute values to replace parameters like @p0, @p1
            var parameterValues = properties
                .Select(p =>
                {
                    var value = p.GetValue(entity);
                    return value == null ? "NULL" : $"'{Sanitize(value.ToString())}'";
                })
                .ToList();

            // Replace placeholders with actual values
            var finalQuery = string.Format(queryTemplate, parameterValues.ToArray());

            try
            {
                using (var command = new OdbcCommand(finalQuery, _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error inserting data into {_tableName}: {ex.Message}", ex);
            }
        }

        // Helper: Sanitize to prevent SQL Injection
        private string Sanitize(string input)
        {
            // Replace single quotes with two single quotes to escape for Hive
            return input.Replace("'", "''");
        }

        // Modified Query Generator
        private (string Query, List<PropertyInfo> Properties) GetCachedEntityData<T>(string tableName)
        {
            if (!EntityCache.TryGetValue(typeof(T), out var cachedData))
            {
                var properties = typeof(T).GetProperties()
                                          .Where(p => !p.GetCustomAttributes(typeof(KeyAttribute), false).Any())
                                          .ToList();

                var columnNames = string.Join(", ", properties
                    .Select(p => p.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() is ColumnAttribute col
                                ? col.Name
                                : p.Name));

                // Generate a format-friendly query template with placeholders like {0}, {1}, ...
                var placeholders = Enumerable.Range(0, properties.Count).Select(i => $"{{{i}}}");
                var parameters = string.Join(", ", placeholders);

                var query = $"INSERT INTO {tableName} ({columnNames}) VALUES ({parameters})";

                cachedData = (query, properties);
                EntityCache[typeof(T)] = cachedData;
            }

            return cachedData;
        }





        private T MapReaderToEntity(DbDataReader reader)
        {
            try
            {
                var entity = Activator.CreateInstance<T>();
                var properties = typeof(T).GetProperties();

                foreach (var property in properties)
                {
                    // Get the ColumnAttribute to find the mapped column name
                    var columnAttribute = property.GetCustomAttributes(typeof(ColumnAttribute), false)
                                                   .FirstOrDefault() as ColumnAttribute;

                    // Use the column name from the attribute or fallback to the property name
                    var columnName = columnAttribute?.Name ?? property.Name;

                    // Check if the reader has the column
                    if (reader.GetSchemaTable().Rows.Cast<DataRow>().Any(row => row["ColumnName"].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (reader[columnName] != DBNull.Value)
                        {
                            property.SetValue(entity, reader[columnName]);
                        }
                    }
                    else
                    {
                        // Log or handle the case where the column does not exist
                        Console.WriteLine($"Column {columnName} not found in the result set.");
                    }
                }

                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error mapping reader to entity: {ex.Message}", ex);
            }
        }


        private T MapReaderToEntityForLatest(DbDataReader reader)
        {
            try
            {
                // Create a new instance of the entity
                var entity = Activator.CreateInstance<T>();
                var properties = typeof(T).GetProperties();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    // Get the column name from the data reader
                    var columnName = reader.GetName(i);

                    // Find a matching property in the entity using ColumnAttribute or property name
                    var property = properties
                        .FirstOrDefault(p =>
                        {
                            var columnAttribute = p.GetCustomAttributes(typeof(ColumnAttribute), false)
                                                   .FirstOrDefault() as ColumnAttribute;
                            return (columnAttribute?.Name ?? p.Name).Equals(columnName, StringComparison.OrdinalIgnoreCase);
                        });

                    if (property != null && !reader.IsDBNull(i))
                    {
                        // Safely set the value
                        var value = reader.GetValue(i);

                        if (value != null && property.PropertyType.IsAssignableFrom(value.GetType()))
                        {
                            property.SetValue(entity, value);
                        }
                        else if (value != null)
                        {
                            // Convert types if necessary
                            property.SetValue(entity, Convert.ChangeType(value, property.PropertyType));
                        }
                    }
                }

                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error mapping reader to entity: {ex.Message}", ex);
            }
        }

        public async Task<T> GetRecordByColumnValueAsync<T>(string tableName, string columnName, object columnValue) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            if (columnValue == null)
                throw new ArgumentNullException(nameof(columnValue), "Column value cannot be null.");

            // Construct query dynamically
            var query = $@"SELECT * FROM {tableName} WHERE {columnName} = ?";

            using (var command = new OdbcCommand(query, _connection))
            {
                // Add parameter to prevent SQL injection
                command.Parameters.AddWithValue("?", columnValue);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Dynamically map the result to the entity
                        return MapReaderToEntity<T>(reader);
                    }
                    else
                    {
                        // Return null if no record is found
                        return null;
                    }
                }
            }
        }

        private T MapReaderToEntity<T>(IDataReader reader) where T : class, new()
        {
            var entity = new T();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttributes(typeof(ColumnAttribute), false)
                                              .FirstOrDefault() as ColumnAttribute;

                var columnName = columnAttribute?.Name ?? property.Name;

                if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                {
                    var value = reader[columnName];

                    // Handle nullable types explicitly
                    var propertyType = property.PropertyType;
                    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                    // Convert value to the appropriate type
                    var convertedValue = Convert.ChangeType(value, underlyingType);

                    // Set the property value
                    property.SetValue(entity, convertedValue);
                }
            }

            return entity;
        }

        public async Task BulkUpdateAsync(IEnumerable<T> entities, string primaryKeyColumn, string additionalWhereColumn, string additionalWhereColumnTwo)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities cannot be null or empty");

            const int batchSize = 100;
            var chunks = entities.Chunk(batchSize);
            var errorMessages = new List<string>();

            foreach (var chunk in chunks)
            {
                foreach (var entity in chunk)
                {
                    try
                    {
                        await BuildUpdateQuery(entity, _tableName, primaryKeyColumn, additionalWhereColumn, additionalWhereColumnTwo);
                    }
                    catch (Exception ex)
                    {
                        // Extract the primary key value
                        var primaryKeyValue = GetPrimaryKeyValue(entity, primaryKeyColumn);

                        // Log exception with primary key context
                        var errorMessage = $"Error updating record with {primaryKeyColumn} = {primaryKeyValue}: {ex.Message}";
                        errorMessages.Add(errorMessage);
                    }
                }
            }

            if (errorMessages.Any())
            {
                // You can log all errors or display them as required
                var aggregatedErrors = string.Join(Environment.NewLine, errorMessages);
                Console.WriteLine("Errors encountered during bulk update:");
                Console.WriteLine(aggregatedErrors);

                // Optionally: Throw an exception with all errors
                throw new InvalidOperationException($"Bulk update completed with errors: {aggregatedErrors}");
            }
        }

        /// <summary>
        /// Retrieves the primary key value from an entity.
        /// </summary>
        private object GetPrimaryKeyValue(T entity, string primaryKeyColumn)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var property = GetCachedProperties(typeof(T))
                            .FirstOrDefault(p => p.ColumnName.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase));

            if (property == null)
                throw new ArgumentException($"Primary key column '{primaryKeyColumn}' not found in entity type '{typeof(T).Name}'.");

            return property.PropertyInfo.GetValue(entity);
        }


        /// <summary>
        /// Builds a single UPDATE query for a given record.
        /// </summary>
        private async Task BuildUpdateQuery(T entity, string tableName, string primaryKeyColumn, string additionalWhereColumn = null, string additionalWhereColumnTwo = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var properties = GetCachedPropertiesIncludingPK(typeof(T)); // GetCachedProperties(typeof(T));
            var setClauses = new List<string>();
            var parameters = new List<OdbcParameter>();

            object primaryKeyValue = null;
            object additionalWhereColumnValue = null;
            object additionalWhereColumnTwoValue = null;

            foreach (var property in properties)
            {
                var columnName = property.ColumnName;
                var value = property.PropertyInfo.GetValue(entity);

                // Handle primary key
                if (columnName.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
                {
                    primaryKeyValue = value;
                    continue;
                }
                // Handle additional WHERE columns
                if (!string.IsNullOrEmpty(additionalWhereColumn) && columnName.Equals(additionalWhereColumn, StringComparison.OrdinalIgnoreCase))
                {
                    additionalWhereColumnValue = value;
                    continue;
                }
                if (!string.IsNullOrEmpty(additionalWhereColumnTwo) && columnName.Equals(additionalWhereColumnTwo, StringComparison.OrdinalIgnoreCase))
                {
                    additionalWhereColumnTwoValue = value;
                    continue;
                }

                // Handle DateTime and nullable values
                if (value == null || value == DBNull.Value)
                {
                    setClauses.Add($"{columnName} = NULL");
                    continue;
                }
                if (property.PropertyInfo.PropertyType == typeof(DateTime) || property.PropertyInfo.PropertyType == typeof(DateTime?))
                {
                    if ((DateTime)value == DateTime.MinValue)
                    {
                        setClauses.Add($"{columnName} = NULL");
                    }
                    else
                    {
                        setClauses.Add($"{columnName} = ?");
                        parameters.Add(new OdbcParameter($"@{columnName}", ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff")));
                    }
                    continue;
                }

                // General value handling
                setClauses.Add($"{columnName} = ?");
                parameters.Add(new OdbcParameter($"@{columnName}", value));
            }

            // Validate the primary key value
            if (primaryKeyValue == null)
                throw new ArgumentException($"Primary key '{primaryKeyColumn}' cannot be null.");

            // Construct the SET clause
            var setClause = string.Join(", ", setClauses);

            // Construct the WHERE clause
            var whereClauses = new List<string> { $"{primaryKeyColumn} = ?" };
            parameters.Add(new OdbcParameter("@PrimaryKey", primaryKeyValue));

            if (!string.IsNullOrEmpty(additionalWhereColumn) && additionalWhereColumnValue != null)
            {
                whereClauses.Add($"{additionalWhereColumn} = ?");
                parameters.Add(new OdbcParameter($"@{additionalWhereColumn}", additionalWhereColumnValue));
            }
            if (!string.IsNullOrEmpty(additionalWhereColumnTwo) && additionalWhereColumnTwoValue != null)
            {
                whereClauses.Add($"{additionalWhereColumnTwo} = ?");
                parameters.Add(new OdbcParameter($"@{additionalWhereColumnTwo}", additionalWhereColumnTwoValue));
            }

            // Final query
            var whereClause = string.Join(" AND ", whereClauses);
            var query = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";

            using (var command = new OdbcCommand(query, _connection))
            {
                command.Parameters.AddRange(parameters.ToArray());

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException($"No record found with {primaryKeyColumn} = '{primaryKeyValue}'" +
                                                        $"{(!string.IsNullOrEmpty(additionalWhereColumn) ? $" and {additionalWhereColumn} = '{additionalWhereColumnValue}'" : string.Empty)}" +
                                                        $"{(!string.IsNullOrEmpty(additionalWhereColumnTwo) ? $" and {additionalWhereColumnTwo} = '{additionalWhereColumnTwoValue}'" : string.Empty)}.");
                }
            }


            //return query;
        }

        public async Task<IEnumerable<TEntity>> GetDBRecordsAsync<TEntity>(
    IEnumerable<TEntity> entities, string primaryKeyColumn) where TEntity : class, new()
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities cannot be null or empty", nameof(entities));

            // Get primary key values dynamically from TEntity
            var primaryKeyValues = entities
                .Select(e =>
                {
                    var properties = ReUsableMethods.GetCachedPropertiesIncludingPK(typeof(TEntity));
                    return properties
                        .FirstOrDefault(p => p.ColumnName.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
                        ?.PropertyInfo
                        .GetValue(e)?.ToString(); // Convert value to string for SQL query
                })
                .Where(val => !string.IsNullOrEmpty(val))
                .Distinct()
                .ToList();

            if (!primaryKeyValues.Any())
                throw new ArgumentException($"No valid primary key values found for the column {primaryKeyColumn}");

            // Handle SQL IN clause with safe string interpolation
            string inClause = string.Join(",", primaryKeyValues.Select(x => $"'{x.Replace("'", "''")}'")); // Prevent SQL Injection

            string query = $"SELECT * FROM {_tableName} WHERE {primaryKeyColumn} IN ({inClause})";

            var items = new List<TEntity>();

            using (var command = new OdbcCommand(query, _connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = ReUsableMethods.MapReaderToEntity<TEntity>(reader); // Now returns any generic entity
                        items.Add(item);
                    }
                }
            }

            return items;
        }


        public async Task<IEnumerable<TEntity>> GetDBRecordsAsync<TEntity>(
            IEnumerable<TEntity> entities,
            string primaryKeyColumn,
            string? sourceColumn = null,
            string? vendorIdColumn = null)
            where TEntity : class, new()
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities cannot be null or empty", nameof(entities));

            // 🟢 STEP 1: Get Available Columns
            var availableColumns = await GetColumnNamesAsync(_tableName);

            bool hasSourceColumn = !string.IsNullOrEmpty(sourceColumn) &&
                                   availableColumns.Contains(sourceColumn, StringComparer.OrdinalIgnoreCase);

            bool hasVendorIdColumn = !string.IsNullOrEmpty(vendorIdColumn) &&
                                     availableColumns.Contains(vendorIdColumn, StringComparer.OrdinalIgnoreCase);

            // 🟢 STEP 2: Extract Key Combinations
            var keyCombinations = entities
                .Select(e =>
                {
                    var properties = ReUsableMethods.GetCachedPropertiesIncludingPK(typeof(TEntity));

                    var primaryKeyValue = properties
                        .FirstOrDefault(p => p.ColumnName.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
                        ?.PropertyInfo.GetValue(e)?.ToString();

                    var sourceValue = hasSourceColumn
                        ? properties.FirstOrDefault(p => p.ColumnName.Equals(sourceColumn, StringComparison.OrdinalIgnoreCase))
                            ?.PropertyInfo.GetValue(e)?.ToString()
                        : null;

                    var vendorIdValue = hasVendorIdColumn
                        ? properties.FirstOrDefault(p => p.ColumnName.Equals(vendorIdColumn, StringComparison.OrdinalIgnoreCase))
                            ?.PropertyInfo.GetValue(e)?.ToString()
                        : null;

                    return new { PrimaryKey = primaryKeyValue, Source = sourceValue, VendorId = vendorIdValue };
                })
                .Where(k => !string.IsNullOrEmpty(k.PrimaryKey))  // Ensure PrimaryKey exists
                .Distinct()
                .ToList();

            if (!keyCombinations.Any())
                throw new ArgumentException($"No valid key values found for column {primaryKeyColumn}");

            // 🟢 STEP 3: Construct Query
            var conditions = new List<string>();
            var parameters = new List<OdbcParameter>();
            int paramIndex = 0;

            foreach (var keyCombo in keyCombinations)
            {
                var conditionParts = new List<string>();

                // **Primary Key (Always Present)**
                parameters.Add(new OdbcParameter($"?{paramIndex}", keyCombo.PrimaryKey));
                conditionParts.Add($"{primaryKeyColumn} = ?");

                // **Source Column (If Available)**
                if (hasSourceColumn && keyCombo.Source != null)
                {
                    parameters.Add(new OdbcParameter($"?{++paramIndex}", keyCombo.Source));
                    conditionParts.Add($"{sourceColumn} = ?");
                }

                // **Vendor ID Column (If Available)**
                if (hasVendorIdColumn && keyCombo.VendorId != null)
                {
                    parameters.Add(new OdbcParameter($"?{++paramIndex}", keyCombo.VendorId));
                    conditionParts.Add($"{vendorIdColumn} = ?");
                }

                conditions.Add($"({string.Join(" AND ", conditionParts)})");
                paramIndex++;
            }

            string query = $"SELECT * FROM {_tableName} WHERE " + string.Join(" OR ", conditions);

            // 🟢 STEP 4: Execute Query with Proper Connection Handling
            var items = new List<TEntity>();

            // 🔥 FIXED: Ensure connection is initialized
            using (var command = new OdbcCommand(query, _connection))
            {
                command.Parameters.AddRange(parameters.ToArray());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = ReUsableMethods.MapReaderToEntity<TEntity>(reader);
                        items.Add(item);
                    }
                }
            }


            return items;
        }




        public IEnumerable<T> Find(Expression<Func<T, bool>> expression)
        {
            return dbSet.Where(expression);
        }

        public async Task<T?> GetById(int id)
        {
            return await dbSet.FindAsync(id);
        }
    }
}
