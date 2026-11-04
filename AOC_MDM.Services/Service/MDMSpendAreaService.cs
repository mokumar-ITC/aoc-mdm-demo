using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;


namespace AOC_MDM.Services.Service
{

    public class MDMSpendAreaService : IMDMSpendAreaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMSpendAreaService> _logger;


        public MDMSpendAreaService(
            IUnitOfWork unitOfWork,
            IMapper mapper
            , ILogger<MDMSpendAreaService> logger
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<MDMSpendAreaDTO>> GetSpendAreaAsync()
        {
            var mdmSpendArea = await _unitOfWork.SpendAreaRepository.GetAllAsync();

            var mDMSpendAreas = mdmSpendArea
             .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
             .ThenBy(x => x.spend_area)                      // Then sort by Level3
             .ToList();


            return _mapper.Map<IEnumerable<MDMSpendAreaDTO>>(mDMSpendAreas);
        }

        public async Task<IEnumerable<MDMSpendAreaDTO>> SearchAllColumnsAsync(string searchText)
        {
            var mdmSpendArea = await _unitOfWork.SpendAreaRepository.SearchAllColumnsAsync(searchText);

            // Filter and sort records
            var mDMSpendAreas = mdmSpendArea
            .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
            .ThenBy(x => x.spend_area)                      // Then sort by Level3
            .ToList();

            return _mapper.Map<IEnumerable<MDMSpendAreaDTO>>(mDMSpendAreas);
        }

        public async Task<MDMSpendAreaDTO> GetFilteredAsync(string id)
        {
            var mdmSpendArea = await _unitOfWork.SpendAreaRepository
                                     .GetFilteredAsync($"spend_area = '{id}'");

            var singleRecord = mdmSpendArea.FirstOrDefault();

            return _mapper.Map<MDMSpendAreaDTO>(singleRecord);
        }

        public async Task<IEnumerable<string>> GetSpendAreaActiveAsync()
        {
            var mdmSpendArea = await _unitOfWork.SpendAreaRepository.GetAllAsync();

            //Active records will in null or active
            var mDMSpendAreas = mdmSpendArea
                .Where(x => x.Enabled == "Yes")
                .Select(u => u.spend_area)
               .ToList();

            return _mapper.Map<IEnumerable<string>>(mDMSpendAreas);
        }

        public async Task<bool> AddSpendAreaAsync(MDMSpendAreaDTO spendAreaDTO)
        {
            try
            {
                var spendAreaEntity = _mapper.Map<MDMSpendArea>(spendAreaDTO);
                await _unitOfWork.SpendAreaRepository.AddAsync(spendAreaEntity);
                // _logger.LogInformation("Successfully added SpendArea entity with ID: {Id}", spendArea.Id);
                return true;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error occurred while adding SpendArea entity.");
                throw;
            }
        }

        public async Task<bool> UpdateDirectlyAsync(string spendArea, MDMSpendAreaDTO updatedRecord, string checkerId, string remarks)
        {
            try
            {
                // Update the main table
                updatedRecord.UpdatedBy = checkerId;
                updatedRecord.UpdatedDate = DateTime.UtcNow;
                updatedRecord.UpdateStatus = "UpdatedDirectly"; //TODO
                updatedRecord.LastModifiedByRole = "Checker";  //TODO

                //Convert DTO to entity
                var updatedRecordEntity = _mapper.Map<MDMSpendArea>(updatedRecord);

                await _unitOfWork.SpendAreaRepository.UpdateAsync(updatedRecordEntity, updatedRecordEntity.spend_area, "spend_area");
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        public async Task BulkUpdateAsync(IEnumerable<MDMSpendAreaDTO> entities, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                List<MDMSpendArea> entitiesToUpdate = new List<MDMSpendArea>();

                // Convert DTOs to entity objects
                var uiEntities = _mapper.Map<IEnumerable<MDMSpendArea>>(entities);

                // Fetch existing records from the database based on primary keys (Level3)
                var dbEntities = await _unitOfWork.SpendAreaRepository.GetDBRecordsAsync(uiEntities, "spend_area");

                var dbEntitiesDict = dbEntities.ToDictionary(x => x.spend_area); // Optimize lookups


                foreach (var uiEntity in uiEntities)
                {
                    if (dbEntitiesDict.TryGetValue(uiEntity.spend_area, out var existingEntity))
                    {
                        // Update only if a record exists
                        existingEntity.Enabled = uiEntity.Enabled;

                        existingEntity.UpdatedBy = checkerId;
                        existingEntity.UpdatedDate = DateTime.UtcNow;
                        existingEntity.IsActive = true;
                        existingEntity.UpdateStatus = "UpdateDirectly";
                        existingEntity.LastModifiedByRole = role;
                        existingEntity.Remarks = remarks;

                        entitiesToUpdate.Add(existingEntity);
                    }
                }

                await _unitOfWork.SpendAreaRepository.BulkUpdateAsync(entitiesToUpdate, "spend_area", null, null); //properties of entity
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateAsync: {ex.Message}");
                throw;
            }
        }
    }
}
