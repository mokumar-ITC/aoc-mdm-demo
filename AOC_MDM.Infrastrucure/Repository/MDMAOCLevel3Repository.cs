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
    public class MDMAOCLevel3Repository : GenericRepository<MDMAOCLevel3>, IMDMAOCLevel3Repository
    {
        string _tableName = "";
        OdbcConnection _connection;
        public MDMAOCLevel3Repository(OdbcConnection connection, string catalog)
        : base(connection, $"{catalog}.mdm.aoc_level_3", catalog)
        {
            _tableName = $"{catalog}.mdm.aoc_level_3";
            _connection = connection;
        }



      
        //public async Task<IEnumerable<MDMAOCLevel3>> GetMultipleFilteredAsync<T>(IEnumerable<T> entities, string primaryKeyColumn)
        //{
        //    if (entities == null || !entities.Any())
        //        throw new ArgumentException("Entities cannot be null or empty", nameof(entities));

        //    // Get primary key values from entities
        //    var primaryKeyValues = entities
        //        .Select(e =>
        //        {
        //            var properties = ReUsableMethods.GetCachedPropertiesIncludingPK(typeof(T));
        //            return properties
        //                .FirstOrDefault(p => p.ColumnName.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
        //                ?.PropertyInfo
        //                .GetValue(e)?.ToString(); // Convert to string for SQL query
        //        })
        //        .Where(val => !string.IsNullOrEmpty(val))
        //        .Distinct() // Remove duplicates
        //        .ToList();

        //    if (!primaryKeyValues.Any())
        //        throw new ArgumentException($"No valid primary key values found for the column {primaryKeyColumn}");

        //    // Build the WHERE clause using safe string interpolation
        //    string inClause = string.Join(",", primaryKeyValues.Select(x => $"'{x.Replace("'", "''")}'")); // Prevent SQL Injection

        //    string query = $"SELECT * FROM {_tableName} WHERE {primaryKeyColumn} IN ({inClause})";

        //    var items = new List<MDMAOCLevel3>();

        //    using (var command = new OdbcCommand(query, _connection))
        //    {
        //        using (var reader = await command.ExecuteReaderAsync())
        //        {
        //            while (await reader.ReadAsync())
        //            {
        //                var item = ReUsableMethods.MapReaderToEntity(reader);
        //                items.Add(item);
        //            }
        //        }
        //    }

        //    return items;
        //}

    
    }
}
