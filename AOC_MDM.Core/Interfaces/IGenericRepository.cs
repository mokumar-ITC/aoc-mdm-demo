using AOC_MDM.Core.Entities;
using System.Data;
using System.Linq.Expressions;

namespace AOC_MDM.Core.Interfaces
{

    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetById(int id);
        Task<IEnumerable<T>> GetAllAsync();

        Task<IEnumerable<T>> SearchAllColumnsAsync(string searchText);

        Task<IEnumerable<T>> GetFilteredAsync(string whereClause);

        Task<T> GetSingleFilteredAsync(string whereClause);

        Task<IEnumerable<T>> GetAllPaginatedAsync(int pageNumber, int pageSize);

        Task<PaginatedResult<T>> GetAllPaginatedAsync(int pageNumber, int pageSize,
        string filterColumn = null,
        string filterValue = null);

        Task<PaginatedResult<T>> GetAllPaginatedAsync(
    int pageNumber,
    int pageSize,
    string filterColumn = null,
    string filterValue = null,
    string sortColumn = "UpdatedDate",  // Default sorting column
    bool sortAscending = false);

        Task<PaginatedResult<T>> GetDataPlusGlobalSearchAsync(int pageNumber, int pageSize,
            string filterColumn = null,
            string filterValue = null,
            string searchText = null);

        Task<PaginatedResult<T>> GetDataPlusGlobalSearchAsync(
    int pageNumber,
    int pageSize,
    string filterColumn = null,
    string filterValue = null,
    string searchText = null,
    string sortColumn = "UpdatedDate",  // Default sorting column
    bool sortAscending = false);

        Task<IEnumerable<T>> GetDataPlusGlobalSearchAsync(
   string filterColumn = null,
   string filterValue = null,
   string searchText = null);

        IEnumerable<T> Find(Expression<Func<T, bool>> expression);

        Task AddAsync(T entity);

        Task UpdateAsync(T entity, object key, string primaryColumnName);

        Task UpdateAsync(T entity, object key, string primaryColumnName, string sourceColumnName = null, string sourceValue = null, string vendorColumnName = null, string vendorValue = null);

        Task BulkUpdateAsync(IEnumerable<T> entities, string primaryKeyColumn, string additionalWhereColumn, string additionalWhereColumnTwo);

        Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string tableName, string columnName, string? filter = null);

        Task<T> GetRecordByColumnValueAsync<T>(string tableName, string columnName, object columnValue) where T : class, new();

        Task<DataTable> GetRecsWithAffectedAttributesAsync(
        string tableName,
        string columnName,
        string columnValue);

        Task<IEnumerable<TEntity>> GetDBRecordsAsync<TEntity>(
     IEnumerable<TEntity> entities, string primaryKeyColumn) where TEntity : class, new();

        Task<IEnumerable<TEntity>> GetDBRecordsAsync<TEntity>(IEnumerable<TEntity> entities,
    string primaryKeyColumn,
    string? sourceColumn = null,
    string? vendorIdColumn = null) where TEntity : class, new();
    }
}
