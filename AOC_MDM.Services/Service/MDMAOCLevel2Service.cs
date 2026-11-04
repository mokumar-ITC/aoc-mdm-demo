using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using System.Data;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations.Schema;


namespace AOC_MDM.Services.Service
{

    public class MDMAOCLevel2Service : IMDMAOCLevel2Service
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMAOCLevel2Service> _logger;
        private readonly string _catalog;

        public MDMAOCLevel2Service(
            IUnitOfWork unitOfWork,
            IMapper mapper
             , ILogger<MDMAOCLevel2Service> logger
             , DatabaseSettings dbSettings // Inject the catalog name via the settings object

            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _catalog = dbSettings.Catalog; // Store the catalog name
        }

        public async Task<IEnumerable<MDMAOCLevel2DTO>> GetAOCLevel2sAsync()
        {
            var mdmAOCLevel2s = await _unitOfWork.AOCLevel2Repository.GetAllAsync();

            var mDMAOCLevel2sFiltered = mdmAOCLevel2s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level2)                      // Then sort by Level3
                .ThenBy(x => x.Level1)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMAOCLevel2DTO>>(mDMAOCLevel2sFiltered);
        }

        public async Task<IEnumerable<MDMViewAOCLevel2DTO>> GetDataWithOrWithoutFilter(string filterColumn, string filterValue, string searchText)
        {
            var mdmAOCLevel2s = await _unitOfWork.ViewAOCLevel2Repository.GetDataPlusGlobalSearchAsync(filterColumn, filterValue, searchText);
           
            var mDMAOCLevel2sFiltered = mdmAOCLevel2s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level2)                      // Then sort by Level3
                .ThenBy(x => x.Level1)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMViewAOCLevel2DTO>>(mDMAOCLevel2sFiltered);

        }


        public async Task<IEnumerable<MDMAOCLevel2DTO>> SearchAllColumnsAsync(string searchText)
        {
            var mdmAOCLevel2s = await _unitOfWork.AOCLevel2Repository.SearchAllColumnsAsync(searchText);

            var mDMAOCLevel2sFiltered = mdmAOCLevel2s
                 .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                 .ThenBy(x => x.Level2)                      // Then sort by Level3
                 .ThenBy(x => x.Level1)                      // Then sort by Level2
                 .ToList();

            return _mapper.Map<IEnumerable<MDMAOCLevel2DTO>>(mDMAOCLevel2sFiltered);
        }

        public async Task<MDMAOCLevel2DTO> GetFilteredAsync(string level2)
        {
            var mdmAOCLevel2s = await _unitOfWork.AOCLevel2Repository
                                     .GetFilteredAsync($"level_2 = '{level2}'");

            var singleRecord = mdmAOCLevel2s.FirstOrDefault();

            return _mapper.Map<MDMAOCLevel2DTO>(singleRecord);
        }


        public async Task<IEnumerable<string>> GetDistinctLEVEL1ActiveAsync()
        {
            var mdmAOCLevel1s = await _unitOfWork.AOCLevel1Repository.GetAllAsync();

            //Active records will in null or active
            var mdmAOCLevel1sFilter = mdmAOCLevel1s
           .Where(x => x.Enabled == "Yes")
                .Select(u => u.Level1)
                .Distinct()
               .ToList();

            return _mapper.Map<IEnumerable<string>>(mdmAOCLevel1sFilter);

        }

        public async Task<IEnumerable<string>> GetDistinctLEVEL2ActiveAsync()
        {
            var mdmAOCLevel2s = await _unitOfWork.AOCLevel2Repository.GetAllAsync();

            //Active records will in null or active
            var mdmAOCLevel2sFilter = mdmAOCLevel2s
           .Where(x => x.Enabled == "Yes")
                .Select(u => u.Level2)
                .Distinct()
               .ToList();

            return _mapper.Map<IEnumerable<string>>(mdmAOCLevel2sFilter);

        }


        //Get the list of level3 which are with Level2 and get total affected records of Vendor/Item
        public async Task<MDMAOCLevel3EditDTO> GetRecsWithAffectedAttributesAsync(string columnValue)
        {
            // string tableName = "mdm_sandbox.mdm.aoc_level_2";
            MDMAOCLevel3EditDTO result = null;

            //Get the list of level3 which are with Level2
            var tableLevel3Name = $"{_catalog}.mdm.aoc_level_3"; //"mdm_sandbox.mdm.aoc_level_3"; // Replace with your actual table name
            string columnName = "level_3";
            var filter = $"level_2 = '{columnValue}'"; // Example filter: Active records only

            var level3Values = await _unitOfWork.VendorRefAttributesRepository.GetDistinctColumnValuesAsync(tableLevel3Name, columnName, filter);

            // Convert IEnumerable<object> to a comma-separated string
            string level3ValuesString = string.Join(", ", level3Values.Select(value => value.ToString()));

            //var tableLevel2Name = "mdm_sandbox.mdm.aoc_level_2";
            if (string.IsNullOrWhiteSpace(level3ValuesString))
            {
                return result;
            }

            //This will get Level3 with count of effectedrecs. So take count values only.
            var mdmAOCLevel3s = await _unitOfWork.AOCLevel3Repository.GetRecsWithAffectedAttributesAsync(tableLevel3Name, columnName, level3ValuesString);



            foreach (DataRow row in mdmAOCLevel3s.Rows)
            {
                result = new MDMAOCLevel3EditDTO
                {

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




        public async Task<bool> UpdateDirectlyAsync(string level2, MDMAOCLevel2DTO updatedRecord, string checkerId, string remarks)
        {
            try
            {

                // Update the main table
                updatedRecord.UpdatedBy = checkerId;
                updatedRecord.UpdatedDate = DateTime.UtcNow;
                updatedRecord.UpdateStatus = "UpdatedDirectly"; //TODO
                updatedRecord.LastModifiedByRole = "Checker";  //TODO

                //Convert DTO to entity
                var updatedRecordEntity = _mapper.Map<MDMAOCLevel2>(updatedRecord);

                await _unitOfWork.AOCLevel2Repository.UpdateAsync(updatedRecordEntity, updatedRecordEntity.Level2, "level_2");
                return true;
            }
            catch (Exception ex) { return false; }

        }


        public async Task BulkUpdateAsync(IEnumerable<MDMAOCLevel2DTO> entities, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                List<MDMAOCLevel2> entitiesToUpdate = new List<MDMAOCLevel2>();

                // Convert DTOs to entity objects
                var uiEntities = _mapper.Map<IEnumerable<MDMAOCLevel2>>(entities);

                // Fetch existing records from the database based on primary keys (Level3)
                var dbEntities = await _unitOfWork.AOCLevel2Repository.GetDBRecordsAsync(uiEntities, "level_2");

                var dbEntitiesDict = dbEntities.ToDictionary(x => x.Level2); // Optimize lookups


                foreach (var uiEntity in uiEntities)
                {
                    if (dbEntitiesDict.TryGetValue(uiEntity.Level2, out var existingEntity))
                    {
                        // Update only if a record exists
                        existingEntity.Level1 = uiEntity.Level1;
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

                await _unitOfWork.AOCLevel2Repository.BulkUpdateAsync(entitiesToUpdate, "level_2", null, null); //properties of entity
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateAsync: {ex.Message}");
                throw;
            }


        }

    }
}
