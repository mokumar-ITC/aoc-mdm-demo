using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;


namespace AOC_MDM.Services.Service
{

    public class MDMViewItemRefAttrsService : IMDMViewItemRefAttrsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMViewItemRefAttrsService> _logger;


        public MDMViewItemRefAttrsService(
            IUnitOfWork unitOfWork,
            IMapper mapper
            , ILogger<MDMViewItemRefAttrsService> logger
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<PaginatedResult<MDMViewItemRefAttrsDTO>> GetViewItemRefAttrsAsync(
 int pageNumber,
 int pageSize,
 string filterColumnName,
 string filterValue, string? sortColumn=null, bool sortAscending=false)
        {
            // Validate pagination parameters
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            // Call the repository method and get paginated results
            //var paginatedResult = await _unitOfWork.ViewItemRefAttrsRepository.GetAllPaginatedAsync(
            //    pageNumber,
            //    pageSize,
            //    filterColumnName,
            //    filterValue);

            var paginatedResult = await _unitOfWork.ViewItemRefAttrsRepository.GetAllPaginatedAsync(
              pageNumber,
              pageSize,
              filterColumnName,
              filterValue, sortColumn, sortAscending);      // "item_description", true); //true is ascending, false is descending

            // Map the data items to DTOs
            var dtoItems = _mapper.Map<IEnumerable<MDMViewItemRefAttrsDTO>>(paginatedResult.Items);

            // Return the results as PaginatedResult<DTO>
            return new PaginatedResult<MDMViewItemRefAttrsDTO>
            {
                Items = dtoItems,
                TotalCount = paginatedResult.TotalCount
            };
        }

        public async Task<PaginatedResult<MDMViewItemRefAttrsDTO>> GetDataPlusGlobalSearchAsync(
       int pageNumber,
       int pageSize,
       string? filterColumnName = null,
       string? filterValue = null,
       string? searchText = null,
       string? sortColumn = null, bool sortAscending = false)
        {
            // Validate pagination parameters
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            // Call the repository method and get paginated results
            //var paginatedResult = await _unitOfWork.ViewItemRefAttrsRepository.GetDataPlusGlobalSearchAsync(
            //    pageNumber,
            //    pageSize,
            //    filterColumnName,
            //    filterValue,
            //    searchText);

            //Call the repository method and get paginated results
            var paginatedResult = await _unitOfWork.ViewItemRefAttrsRepository.GetDataPlusGlobalSearchAsync(
                pageNumber,
                pageSize,
                filterColumnName,
                filterValue,
                searchText, sortColumn, sortAscending);


            // Map the data items to DTOs
            var dtoItems = _mapper.Map<IEnumerable<MDMViewItemRefAttrsDTO>>(paginatedResult.Items);

            // Return the results as PaginatedResult<DTO>
            return new PaginatedResult<MDMViewItemRefAttrsDTO>
            {
                Items = dtoItems,
                TotalCount = paginatedResult.TotalCount
            };
        }

    }
}
