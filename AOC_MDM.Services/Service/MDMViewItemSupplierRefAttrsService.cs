using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;


namespace AOC_MDM.Services.Service
{

    public class MDMViewItemSupplierRefAttrsService : IMDMViewItemSupplierRefAttrsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMViewItemSupplierRefAttrsService> _logger;


        public MDMViewItemSupplierRefAttrsService(
            IUnitOfWork unitOfWork,
            IMapper mapper
            , ILogger<MDMViewItemSupplierRefAttrsService> logger
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<PaginatedResult<MDMViewItemSupplierRefAttrsDTO>> GetViewItemSupplierRefAttrsAsync(
    int pageNumber,
    int pageSize,
    string filterColumnName,
    string filterValue)
        {
            // Validate pagination parameters
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            // Call the repository method and get paginated results
            var paginatedResult = await _unitOfWork.ViewItemSupplierRefAttrsRepository.GetAllPaginatedAsync(
                pageNumber,
                pageSize,
                filterColumnName,
                filterValue);

            // Map the data items to DTOs
            var dtoItems = _mapper.Map<IEnumerable<MDMViewItemSupplierRefAttrsDTO>>(paginatedResult.Items);

            // Return the results as PaginatedResult<DTO>
            return new PaginatedResult<MDMViewItemSupplierRefAttrsDTO>
            {
                Items = dtoItems,
                TotalCount = paginatedResult.TotalCount
            };
        }

        public async Task<PaginatedResult<MDMViewItemSupplierRefAttrsDTO>> GetDataPlusGlobalSearchAsync(int pageNumber, int pageSize,
            string? filterColumn, string? filterValue, string? searchText)
        {
            // Validate pagination parameters
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            // Call the repository method and get paginated results
            var paginatedResult = await _unitOfWork.ViewItemSupplierRefAttrsRepository.GetDataPlusGlobalSearchAsync(
                pageNumber,
                pageSize,
                filterColumn,
                filterValue, searchText);

            // Map the data items to DTOs
            var dtoItems = _mapper.Map<IEnumerable<MDMViewItemSupplierRefAttrsDTO>>(paginatedResult.Items);

            // Return the results as PaginatedResult<DTO>
            return new PaginatedResult<MDMViewItemSupplierRefAttrsDTO>
            {
                Items = dtoItems,
                TotalCount = paginatedResult.TotalCount
            };
        }
    }
}
