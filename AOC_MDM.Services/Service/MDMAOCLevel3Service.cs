using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using System.Data;
using System.Diagnostics;


namespace AOC_MDM.Services.Service
{

    public class MDMAOCLevel3Service : IMDMAOCLevel3Service
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMAOCLevel3Service> _logger;
        private readonly string _catalog;


        public MDMAOCLevel3Service(
            IUnitOfWork unitOfWork,
            IMapper mapper
             , ILogger<MDMAOCLevel3Service> logger
         , DatabaseSettings dbSettings // Inject the catalog name via the settings object

            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _catalog = dbSettings.Catalog; // Store the catalog name
        }

        public async Task<IEnumerable<MDMAOCLevel3DTO>> GetAOCLevel3sAsync()
        {
            var mdmAOCLevel3s = await _unitOfWork.AOCLevel3Repository.GetAllAsync();

            // Filter and sort records
            var mDMAOCLevel3sFiltered = mdmAOCLevel3s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level3)                      // Then sort by Level3
                .ThenBy(x => x.Level2)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMAOCLevel3DTO>>(mDMAOCLevel3sFiltered);
        }

        public async Task<IEnumerable<MDMViewAOCLevel3DTO>> GetDataWithOrWithoutFilter(string filterColumn, string filterValue, string searchText)
        {
            var mdmAOCLevel3s = await _unitOfWork.ViewAOCLevel3Repository.GetDataPlusGlobalSearchAsync(filterColumn, filterValue, searchText);

            // Filter and sort records
            var mDMAOCLevel3sFiltered = mdmAOCLevel3s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level3)                      // Then sort by Level3
                .ThenBy(x => x.Level2)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMViewAOCLevel3DTO>>(mDMAOCLevel3sFiltered);

        }


        public async Task<IEnumerable<MDMAOCLevel3DTO>> SearchAllColumnsAsync(string searchText)
        {
            var mdmAOCLevel3s = await _unitOfWork.AOCLevel3Repository.SearchAllColumnsAsync(searchText);

            // Filter and sort records
            var mDMAOCLevel3sFiltered = mdmAOCLevel3s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level3)                      // Then sort by Level3
                .ThenBy(x => x.Level2)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMAOCLevel3DTO>>(mDMAOCLevel3sFiltered);
        }


        public async Task<IEnumerable<string>> GetDistinctLEVEL3ActiveAsync()
        {
            var mdmAOCLevel3s = await _unitOfWork.AOCLevel3Repository.GetAllAsync();

            //Active records will in null or active
            var mdmAOCLevel3sFilter = mdmAOCLevel3s
           .Where(x => x.Enabled == "Yes")
                .Select(u => u.Level3)
                .Distinct()
               .ToList();

            return _mapper.Map<IEnumerable<string>>(mdmAOCLevel3sFilter);

        }


        public async Task<MDMAOCLevel3DTO> GetFilteredAsync(string level3)
        {
            var mdmAOCLevel3s = await _unitOfWork.AOCLevel3Repository
                                     .GetFilteredAsync($"level_3 = '{level3}'");

            var singleRecord = mdmAOCLevel3s.FirstOrDefault();

            return _mapper.Map<MDMAOCLevel3DTO>(singleRecord);
        }

        public async Task<MDMAOCLevel3EditDTO> GetRecsWithAffectedAttributesAsync(string columnValue)
        {
            string tableName = $"{_catalog}.mdm.aoc_level_3"; // "mdm_sandbox.mdm.aoc_level_3";
            string columnName = "level_3";

            var mdmAOCLevel3s = await _unitOfWork.AOCLevel3Repository.GetRecsWithAffectedAttributesAsync(tableName, columnName, columnValue);

            MDMAOCLevel3EditDTO result = null;

            foreach (DataRow row in mdmAOCLevel3s.Rows)
            {
                result = new MDMAOCLevel3EditDTO
                {
                    Level3 = row["level_3"]?.ToString(),
                    Level2 = row["level_2"]?.ToString(),
                    Feedstock1 = row["feedstock_1"]?.ToString(),
                    Feedstock2 = row["feedstock_2"]?.ToString(),
                    Feedstock3 = row["feedstock_3"]?.ToString(),
                    Hazardous = row["hazardous"]?.ToString(),
                    ConflictMineralForcedLaborRegCheck = row["conflict_mineral_forced_labor_reg_check"]?.ToString(),
                    Enabled = row["enabled"]?.ToString(),
                    IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
                    UpdateStatus = row["UpdateStatus"]?.ToString(),
                    LastModifiedByRole = row["LastModifiedByRole"]?.ToString(),
                    Remarks = row["Remarks"]?.ToString(),
                    TotalNoOfAffectedRecsOfVendor = row["Total_Effected_Recs_of_Vendor"] != DBNull.Value
                        ? Convert.ToInt32(row["Total_Effected_Recs_of_Vendor"])
                        : 0,
                    TotalNoOfAffectedRecsOfItem = row["Total_Effected_Recs_of_Item"] != DBNull.Value
                        ? Convert.ToInt32(row["Total_Effected_Recs_of_Item"])
                        : 0
                };
            }

            return result;

        }



        public async Task<bool> UpdateDirectlyAsync(string level3, MDMAOCLevel3DTO updatedRecord, string checkerId, string remarks)
        {
            try
            {

                // Update the main table
                updatedRecord.UpdatedBy = checkerId;
                updatedRecord.UpdatedDate = DateTime.UtcNow;
                updatedRecord.UpdateStatus = "UpdatedDirectly"; //TODO
                updatedRecord.LastModifiedByRole = "Checker";  //TODO

                //Convert DTO to entity
                var updatedRecordEntity = _mapper.Map<MDMAOCLevel3>(updatedRecord);

                await _unitOfWork.AOCLevel3Repository.UpdateAsync(updatedRecordEntity, updatedRecordEntity.Level3, "level_3");

                return true;
            }
            catch (Exception e) { return false; }
        }

        public async Task BulkUpdateAsync(IEnumerable<MDMAOCLevel3DTO> entities, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                // Create a new Stopwatch instance
               // Stopwatch stopwatch = Stopwatch.StartNew();

                List<MDMAOCLevel3> entitiesToUpdate = new List<MDMAOCLevel3>();

                // Convert DTOs to entity objects
                var uiEntities = _mapper.Map<IEnumerable<MDMAOCLevel3>>(entities);

                // Fetch existing records from the database based on primary keys (Level3)
                var dbEntities = await _unitOfWork.AOCLevel3Repository.GetDBRecordsAsync(uiEntities, "level_3");

                var dbEntitiesDict = dbEntities.ToDictionary(x => x.Level3); // Optimize lookups


                foreach (var uiEntity in uiEntities)
                {
                    if (dbEntitiesDict.TryGetValue(uiEntity.Level3, out var existingEntity))
                    {
                        // Update only if a record exists
                        existingEntity.Level2 = uiEntity.Level2;
                        existingEntity.Feedstock1 = uiEntity.Feedstock1;
                        existingEntity.Feedstock2 = uiEntity.Feedstock2;
                        existingEntity.Feedstock3 = uiEntity.Feedstock3;
                        existingEntity.ConflictMineralForcedLaborRegCheck = uiEntity.ConflictMineralForcedLaborRegCheck;
                        existingEntity.Hazardous = uiEntity.Hazardous;

                        existingEntity.UpdatedBy = checkerId;
                        existingEntity.UpdatedDate = DateTime.UtcNow;
                        existingEntity.IsActive = true;
                        existingEntity.UpdateStatus = "UpdateDirectly";
                        existingEntity.LastModifiedByRole = role;
                        existingEntity.Remarks = remarks;

                        entitiesToUpdate.Add(existingEntity);
                    }
                }

                // Stop the stopwatch
                //stopwatch.Stop();

                //// Get the elapsed time in seconds
                //double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                //Console.WriteLine($"BulkUpdateAsync Method execution time: {elapsedSeconds} seconds.");

                await _unitOfWork.AOCLevel3Repository.BulkUpdateAsync(entitiesToUpdate, "level_3", null, null); //properties of entity
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateAsync: {ex.Message}");
                throw;
            }


        }


    }
}
