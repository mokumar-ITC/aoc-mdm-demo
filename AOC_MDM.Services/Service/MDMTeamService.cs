using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;


namespace AOC_MDM.Services.Service
{

    public class MDMTeamService : IMDMTeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMTeamService> _logger;
        private readonly string _catalog;

        public MDMTeamService(
            IUnitOfWork unitOfWork,
            IMapper mapper
            , ILogger<MDMTeamService> logger
            , DatabaseSettings dbSettings // Inject the catalog name via the settings object


            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _catalog = dbSettings.Catalog; // Store the catalog name
        }

        public async Task<IEnumerable<MDMTeamDTO>> GetTeamsAsync()
        {
            var mdmTeams = await _unitOfWork.TeamRepository.GetAllAsync();

            var mDMSpendAreas = mdmTeams
              .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
              .ThenBy(x => x.Team)                      // Then sort by Level3
              .ThenBy(x => x.SpendArea)                      // Then sort by Level2
              .ToList();

            return _mapper.Map<IEnumerable<MDMTeamDTO>>(mDMSpendAreas);
        }

        public async Task<IEnumerable<MDMTeamDTO>> SearchAllColumnsAsync(string searchText)
        {
            var mdmTeams = await _unitOfWork.TeamRepository.SearchAllColumnsAsync(searchText);

            // Filter and sort records
            var mdmTeamsFiltered = mdmTeams
              .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
              .ThenBy(x => x.Team)                      // Then sort by Level3
              .ThenBy(x => x.SpendArea)                      // Then sort by Level2
              .ToList();

            return _mapper.Map<IEnumerable<MDMTeamDTO>>(mdmTeamsFiltered);
        }

        public async Task<IEnumerable<MDMTeamDTO>> GetDataWithOrWithoutFilter(string filterColumn, string filterValue, string searchText)
        {
            var mdmTeams = await _unitOfWork.TeamRepository.GetDataPlusGlobalSearchAsync(filterColumn, filterValue, searchText);

            var mdmTeamsFiltered = mdmTeams
              .OrderByDescending(x => x.Enabled == "Yes") // Prioritize records with Enabled = "Yes"
              .ThenBy(x => x.Team)                      // Then sort by Level3
              .ThenBy(x => x.SpendArea)                      // Then sort by Level2
              .ToList();

            return _mapper.Map<IEnumerable<MDMTeamDTO>>(mdmTeamsFiltered);

        }

        public async Task<MDMTeamDTO> GetFilteredAsync(string team)
        {
            var mdmTeam = await _unitOfWork.TeamRepository
                                     .GetFilteredAsync($"team = '{team}'");

            var singleRecord = mdmTeam.FirstOrDefault();

            return _mapper.Map<MDMTeamDTO>(singleRecord);
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


        public async Task<bool> UpdateDirectlyAsync(string team, MDMTeamDTO updatedRecord, string checkerId, string remarks)
        {
            try
            {

                // Update the main table
                updatedRecord.UpdatedBy = checkerId;
                updatedRecord.UpdatedDate = DateTime.UtcNow;
                updatedRecord.UpdateStatus = "UpdatedDirectly"; //TODO
                updatedRecord.LastModifiedByRole = "Checker";  //TODO

                //Convert DTO to entity
                var updatedRecordEntity = _mapper.Map<MDMTeam>(updatedRecord);

                await _unitOfWork.TeamRepository.UpdateAsync(updatedRecordEntity, updatedRecordEntity.Team, "team");
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        public async Task BulkUpdateAsync(IEnumerable<MDMTeamDTO> entities, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                //// Create a new Stopwatch instance
                //Stopwatch stopwatch = Stopwatch.StartNew();

                List<MDMTeam> entitiesToUpdate = new List<MDMTeam>();

                // Convert DTOs to entity objects
                var uiEntities = _mapper.Map<IEnumerable<MDMTeam>>(entities);

                // Fetch existing records from the database based on primary keys (Level3)
                var dbEntities = await _unitOfWork.TeamRepository.GetDBRecordsAsync(uiEntities, "team");

                var dbEntitiesDict = dbEntities.ToDictionary(x => x.Team); // Optimize lookups


                foreach (var uiEntity in uiEntities)
                {
                    if (dbEntitiesDict.TryGetValue(uiEntity.Team, out var existingEntity))
                    {
                        // Update only if a record exists
                        existingEntity.SpendArea = uiEntity.SpendArea;
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

                // Stop the stopwatch
                //stopwatch.Stop();

                //// Get the elapsed time in seconds
                //double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                //Console.WriteLine($"BulkUpdateAsync Method execution time: {elapsedSeconds} seconds.");

                await _unitOfWork.TeamRepository.BulkUpdateAsync(entitiesToUpdate, "team", null, null); //properties of entity
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateAsync: {ex.Message}");
                throw;
            }
        }



    }
}
