using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;


namespace AOC_MDM.Services.Service
{

    public class MDMViewVendorRefAttrsService : IMDMViewVendorRefAttrsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMViewVendorRefAttrsService> _logger;


        public MDMViewVendorRefAttrsService(
            IUnitOfWork unitOfWork,
            IMapper mapper
            , ILogger<MDMViewVendorRefAttrsService> logger
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PaginatedResult<MDMViewVendorRefAttrsDTO>> GetViewVendorRefAttrsAsync(
    int pageNumber,
    int pageSize,
    string filterColumnName,
    string filterValue)
        {
            try
            {
                _logger.LogInformation($"Starting GetViewVendorRefAttrsAsync method for pageno, pagesize: {pageNumber} ,{pageSize}");

                // Validate pagination parameters
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                // Call the repository method and get paginated results
                var paginatedResult = await _unitOfWork.ViewVendorRefAttrsRepository.GetAllPaginatedAsync(
                    pageNumber,
                    pageSize,
                    filterColumnName,
                    filterValue);

                // Map the data items to DTOs
                var dtoItems = _mapper.Map<IEnumerable<MDMViewVendorRefAttrsDTO>>(paginatedResult.Items);


                // Return the results as PaginatedResult<DTO>
                return new PaginatedResult<MDMViewVendorRefAttrsDTO>
                {
                    Items = dtoItems,
                    TotalCount = paginatedResult.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while while executing method, GetViewVendorRefAttrsAsync");
                return null;
            }
        }

        public async Task<PaginatedResult<MDMViewVendorRefAttrsDTO>> GetDataPlusGlobalSearchAsync(
        int pageNumber,
        int pageSize,
        string? filterColumnName = null,
        string? filterValue = null,
        string? searchText = null)
        {
            try
            {

                // Validate pagination parameters
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                // Call the repository method and get paginated results
                var paginatedResult = await _unitOfWork.ViewVendorRefAttrsRepository.GetDataPlusGlobalSearchAsync(
                    pageNumber,
                    pageSize,
                    filterColumnName,
                    filterValue,
                    searchText);

                // Map the data items to DTOs
                var dtoItems = _mapper.Map<IEnumerable<MDMViewVendorRefAttrsDTO>>(paginatedResult.Items);


                // Return the results as PaginatedResult<DTO>
                return new PaginatedResult<MDMViewVendorRefAttrsDTO>
                {
                    Items = dtoItems,
                    TotalCount = paginatedResult.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while while executing method, GetViewVendorRefAttrsAsync");
                return null;
            }
        }

    }
}
