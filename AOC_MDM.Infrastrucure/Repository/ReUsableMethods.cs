using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using System.Data.Odbc;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Data.Common;
using System.Data;

namespace AOC_MDM.Infrastrucure.Repository
{
    public static class ReUsableMethods
    {

        // Cache entity property metadata to improve performance
        private static readonly ConcurrentDictionary<Type, List<PropertyMapping>> _propertyCache = new();

       

        public static List<PropertyMapping> GetCachedProperties(Type type)
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

        public static TEntity MapReaderToEntity<TEntity>(IDataReader reader) where TEntity : class, new()
        {
            var entity = new TEntity();
            var properties = typeof(TEntity).GetProperties();

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


        public static MDMAOCLevel3 MapReaderToEntity(DbDataReader reader)
        {
            try
            {
                var entity = Activator.CreateInstance<MDMAOCLevel3>();
                var properties = typeof(MDMAOCLevel3).GetProperties();

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

        public static List<PropertyMapping> GetCachedPropertiesIncludingPK(Type type)
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


        private static async Task<IEnumerable<string>> GetColumnNamesAsync(OdbcConnection connection, string tableName)
        {
            var columnNames = new List<string>();

            // PostgreSQL: read column metadata from information_schema (replaces Databricks DESCRIBE)
            var parts = tableName.Split('.');
            const string query = "SELECT column_name FROM information_schema.columns WHERE table_schema = ? AND table_name = ? ORDER BY ordinal_position";
            using (var command = new OdbcCommand(query, connection))
            {
            command.Parameters.Add(new OdbcParameter("@schema", (parts.Length >= 2 ? parts[^2] : "public").ToLowerInvariant()));
            command.Parameters.Add(new OdbcParameter("@table", parts[^1].ToLowerInvariant()));
            using (var reader = await command.ExecuteReaderAsync())
            {
                // Read the results and extract column names
                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString(0); // First column contains the column name
                    columnNames.Add(columnName);
                }
            }
            }
            return columnNames;
        }


    }
    public class PropertyMapping
    {
        public string ColumnName { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public bool IsKey { get; set; }
    }
}
