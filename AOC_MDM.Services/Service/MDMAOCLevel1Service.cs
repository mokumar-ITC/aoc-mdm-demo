using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using System.Data;
using System.Linq;


namespace AOC_MDM.Services.Service
{

    public class MDMAOCLevel1Service : IMDMAOCLevel1Service
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMAOCLevel1Service> _logger;
        private readonly string _catalog;

        public MDMAOCLevel1Service(
            IUnitOfWork unitOfWork,
            IMapper mapper
            , ILogger<MDMAOCLevel1Service> logger
            , DatabaseSettings dbSettings // Inject the catalog name via the settings object

            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _catalog = dbSettings.Catalog; // Store the catalog name
            _logger = logger;
        }

        public async Task<IEnumerable<MDMAOCLevel1DTO>> GetAOCLevel1sAsync()
        {
            var mdmAOCLevel1s = await _unitOfWork.AOCLevel1Repository.GetAllAsync();

            var mDMAOCLevel1sFiltered = mdmAOCLevel1s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level1)                      // Then sort by Level3
                .ThenBy(x => x.Team)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMAOCLevel1DTO>>(mDMAOCLevel1sFiltered);
        }

        public async Task<IEnumerable<MDMViewAOCLevel1DTO>> GetViewAOCLevel1sAsync()
        {
            var mdmAOCLevel1s = await _unitOfWork.ViewAOCLevel1Repository.GetAllAsync();

            var mDMAOCLevel1sFiltered = mdmAOCLevel1s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level1)                      // Then sort by Level3
                .ThenBy(x => x.Team)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMViewAOCLevel1DTO>>(mDMAOCLevel1sFiltered);
        }

        public async Task<IEnumerable<MDMAOCLevel1DTO>> SearchAllColumnsAsync(string searchText)
        {
            var mdmAOCLevel1s = await _unitOfWork.AOCLevel1Repository.SearchAllColumnsAsync(searchText);

            var mDMAOCLevel1sFiltered = mdmAOCLevel1s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level1)                      // Then sort by Level3
                .ThenBy(x => x.Team)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMAOCLevel1DTO>>(mDMAOCLevel1sFiltered);
        }

        public async Task<IEnumerable<MDMViewAOCLevel1DTO>> GetDataWithOrWithoutFilter(string filterColumn,string filterValue,string searchText)
        {
            var mdmAOCLevel1s = await _unitOfWork.ViewAOCLevel1Repository.GetDataPlusGlobalSearchAsync(filterColumn, filterValue, searchText);
                //SearchAllColumnsAsync(searchText);

            var mDMAOCLevel1sFiltered = mdmAOCLevel1s
                .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
                .ThenBy(x => x.Level1)                      // Then sort by Level3
                .ThenBy(x => x.Team)                      // Then sort by Level2
                .ToList();

            return _mapper.Map<IEnumerable<MDMViewAOCLevel1DTO>>(mDMAOCLevel1sFiltered);
        }


        public async Task<MDMAOCLevel1DTO> GetFilteredAsync(string level1)
        {
            var mdmAOCLevel1s = await _unitOfWork.AOCLevel1Repository
                                     .GetFilteredAsync($"level_1 = '{level1}'");

            var singleRecord = mdmAOCLevel1s.FirstOrDefault();

            return _mapper.Map<MDMAOCLevel1DTO>(singleRecord);
        }


        public async Task<IEnumerable<string>> GetDistinctTeamsActiveAsync()
        {
            var mdmTeams = await _unitOfWork.TeamRepository.GetAllAsync();

            //Active records will in null or active
            var mdmTeamsFilter = mdmTeams
                .Where(x => x.Enabled == "Yes")
                .Select(u => u.Team)
                .Distinct()
               .ToList();

            return _mapper.Map<IEnumerable<string>>(mdmTeamsFilter);
        }


        //Get the list of level3 which are with Level2 and get total affected records of Vendor/Item
        public async Task<MDMAOCLevel3EditDTO> GetRecsWithAffectedAttributesAsync(string columnValue)
        {
            // string tableName = "mdm_sandbox.mdm.aoc_level_2";
            MDMAOCLevel3EditDTO result = null;

            //Get list of Level 2 from Level2 table based on Level1 value


            var mdmAOCLevel2s = await _unitOfWork.AOCLevel2Repository.GetAllAsync();

            //Active records will in null or active
            var mdmAOCLevel2sFilter = mdmAOCLevel2s.
                 Where(x => x.Level1 == columnValue)
                 .Select(u => u.Level2)
               .Distinct()
                .ToList();


            //Get list of Level3 basd on Level 2 values
            var mdmAOCLevel3s = await _unitOfWork.AOCLevel3Repository.GetAllAsync();

            // Filter Level3 records where Level2 matches any value in mdmAOCLevel2sFilter
            var mdmAOCLevel3sFilter = mdmAOCLevel3s
                .Where(x => mdmAOCLevel2sFilter.Contains(x.Level2)) // Correct filtering condition
                .Select(u => u.Level3)
                .Distinct()
                .ToList();


            //Pass these list of Level 3 to method which returns total no of affected Item/Vendor
            var tableLevel3Name = $"{_catalog}.mdm.aoc_level_3"; //"mdm_sandbox.mdm.aoc_level_3"; // Replace with your actual table name
            string columnName = "level_3";
            var filter = $"level_2 = '{columnValue}'"; // Example filter: Active records only

            // Convert IEnumerable<object> to a comma-separated string
            string level3ValuesString = string.Join(", ", mdmAOCLevel3sFilter.Select(value => value.ToString()));

            //var tableLevel2Name = "mdm_sandbox.mdm.aoc_level_2";
            if (string.IsNullOrWhiteSpace(level3ValuesString))
            {
                return result;
            }

            //This will get Level3 with count of effectedrecs. So take count values only.
            var mdmAOCLevel3sEffectedRows = await _unitOfWork.AOCLevel3Repository.GetRecsWithAffectedAttributesAsync(tableLevel3Name, columnName, level3ValuesString);



            foreach (DataRow row in mdmAOCLevel3sEffectedRows.Rows)
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





        public async Task<bool> UpdateDirectlyAsync(string level1, MDMAOCLevel1DTO updatedRecord, string checkerId, string remarks)
        {
            try
            {

                // Update the main table
                updatedRecord.UpdatedBy = checkerId;
                updatedRecord.UpdatedDate = DateTime.UtcNow;
                updatedRecord.UpdateStatus = "UpdatedDirectly"; //TODO
                updatedRecord.LastModifiedByRole = "Checker";  //TODO

                //Convert DTO to entity
                var updatedRecordEntity = _mapper.Map<MDMAOCLevel1>(updatedRecord);

                await _unitOfWork.AOCLevel1Repository.UpdateAsync(updatedRecordEntity, updatedRecordEntity.Level1, "level_1");
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        public async Task BulkUpdateAsync(IEnumerable<MDMAOCLevel1DTO> entities, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                List<MDMAOCLevel1> entitiesToUpdate = new List<MDMAOCLevel1>();

                // Convert DTOs to entity objects
                var uiEntities = _mapper.Map<IEnumerable<MDMAOCLevel1>>(entities);

                // Fetch existing records from the database based on primary keys (Level3)
                var dbEntities = await _unitOfWork.AOCLevel1Repository.GetDBRecordsAsync(uiEntities, "level_1");

                var dbEntitiesDict = dbEntities.ToDictionary(x => x.Level1); // Optimize lookups


                foreach (var uiEntity in uiEntities)
                {
                    if (dbEntitiesDict.TryGetValue(uiEntity.Level1, out var existingEntity))
                    {
                        // Update only if a record exists
                        existingEntity.Team = uiEntity.Team;
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
                             
                await _unitOfWork.AOCLevel1Repository.BulkUpdateAsync(entitiesToUpdate, "level_1", null, null); //properties of entity
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateAsync: {ex.Message}");
                throw;
            }
        }

    }
}
