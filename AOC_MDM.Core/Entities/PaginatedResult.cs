using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AOC_MDM.Core.Entities
{
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int pageSize { get; set; }
        public int pageNumber { get; set; }
        public string filterColumnName { get; set; }
        public string filterValue { get; set; }

        public int toPageNumber { get; set; }
        public int fromPageNumber { get; set; }
        public int totalPages { get; set; }
        public string search { get; set; }
        public string selectedPageColumns { get; set; }
    }
}
