using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;


namespace AOC_MDM.Services.Service
{

    public class MDMViewAOCLevel1Service : IMDMViewAOCLevel1Service
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMViewAOCLevel1Service> _logger;


        public MDMViewAOCLevel1Service(
            IUnitOfWork unitOfWork,
            IMapper mapper
            , ILogger<MDMViewAOCLevel1Service> logger
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        //For pagination results - not required. In future, it might required
        public async Task<PaginatedResult<MDMViewAOCLevel1DTO>> GetViewAOCLevel1Async(
    int pageNumber,
    int pageSize,
    string filterColumnName,
    string filterValue)
        {
            try
            {
                _logger.LogInformation($"Starting GetViewAOCLevel1Async method for pageno, pagesize: {pageNumber} ,{pageSize}");

                // Validate pagination parameters
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                // Call the repository method and get paginated results
                var paginatedResult = await _unitOfWork.ViewAOCLevel1Repository.GetAllPaginatedAsync(
                    pageNumber,
                    pageSize,
                    filterColumnName,
                    filterValue);

                var mDMAOCLevel1sSorting = paginatedResult.Items
                    .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                    .ThenBy(x => x.Level1)                      // Then sort by Level1
                    .ThenBy(x => x.Team)                      // Then sort by Team
                    .ToList();


                // Map the data items to DTOs
                var dtoItems = _mapper.Map<IEnumerable<MDMViewAOCLevel1DTO>>(mDMAOCLevel1sSorting);


                // Return the results as PaginatedResult<DTO>
                return new PaginatedResult<MDMViewAOCLevel1DTO>
                {
                    Items = dtoItems,
                    TotalCount = paginatedResult.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while while executing method, GetViewAOCLevel1Async");
                return null;
            }
        }

        public async Task<PaginatedResult<MDMViewAOCLevel1DTO>> GetDataPlusGlobalSearchAsync(
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
                var paginatedResult = await _unitOfWork.ViewAOCLevel1Repository.GetDataPlusGlobalSearchAsync(
                    pageNumber,
                    pageSize,
                    filterColumnName,
                    filterValue,
                    searchText);

                // Map the data items to DTOs
                var dtoItems = _mapper.Map<IEnumerable<MDMViewAOCLevel1DTO>>(paginatedResult.Items);


                // Return the results as PaginatedResult<DTO>
                return new PaginatedResult<MDMViewAOCLevel1DTO>
                {
                    Items = dtoItems,
                    TotalCount = paginatedResult.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while while executing method, GetViewAOCLevel1Async");
                return null;
            }
        }

    }
}
