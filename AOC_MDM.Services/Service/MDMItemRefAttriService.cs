using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;


namespace AOC_MDM.Services.Service
{

    public class MDMItemRefAttriService : IMDMItemRefAttrsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMItemRefAttriService> _logger;
        private readonly string _catalog;
        private readonly IColumnEditService _columnEdit;


        public MDMItemRefAttriService(
            IUnitOfWork unitOfWork,
            IMapper mapper
             , ILogger<MDMItemRefAttriService> logger
             , DatabaseSettings dbSettings // Inject the catalog name via the settings object
            , IColumnEditService columnEdit

     )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _catalog = dbSettings.Catalog; // Store the catalog name
            _columnEdit = columnEdit;
        }

        public async Task<MDMItemRefAttriApprovalDTO> GetApprovalAsync(string itemId, string source)
        {
            MDMItemRefAttriApprovalDTO obj = new MDMItemRefAttriApprovalDTO();

            if (itemId == "0")
            {
                obj.AuditTable = new MDMItemRefAttributesAudit();
                obj.MainTable = new MDMItemRefAttributes();
                return obj;
            }


            //// Fetch main table attributes
            //var mainTableAttributes = await _unitOfWork.ItemRefAttributesRepository.GetAllAsync();

            //// Filter active records for the main table
            //var itemRefAttributesFiltered = mainTableAttributes
            //    .Where(x => x.IsActive != false && x.ItemId == itemId)
            //    .ToList();

            //var itemRefAttributesFiltered = await _unitOfWork.ItemRefAttributesRepository
            //                        .GetFilteredAsync($"item_id = '{itemId}'");

            //var singleRecord = itemRefAttributes.FirstOrDefault();


            var filterQuery = $"item_id = '{itemId}' AND source = '{source}'";

            var itemRefAttributesFiltered = await _unitOfWork.ItemRefAttributesRepository.GetSingleFilteredAsync(filterQuery);

            // If the Main table record isn't found, return null or handle as appropriate
            if (itemRefAttributesFiltered == null)
            {
                return obj; // Or return default empty DTO based on your design preference.
            }


            // Fetch audit table attributes
            //var auditTableAttributes = await _unitOfWork.ItemRefAttriAuditRepository.GetAllAsync();

            // Filter active records for the audit table // x.IsActive != false && TODO
            //var auditTableAttributesFiltered = auditTableAttributes
            //    .Where(x => x.ItemId == itemId && x.ApprovalStatus == "PendingApproval")
            //    .ToList();

            //// LINQ query to join main table with the latest audit record
            //var result = (from main in itemRefAttributesFiltered
            //              join audit in
            //                  (from a in auditTableAttributesFiltered
            //                   group a by a.ItemId into auditGroup
            //                   let latestRecord = auditGroup
            //                       .OrderByDescending(a => a.ChangeTimestamp)
            //                       .FirstOrDefault()
            //                   select latestRecord)
            //              on main.ItemId equals audit.ItemId
            //              select new MDMItemRefAttriApprovalDTO
            //              {
            //                  MainTable = main,
            //                  AuditTable = audit
            //              })
            //             .FirstOrDefault(); // Take only the first match, as we are returning a single DTO.

            //// Return the result
            //return result;

            // Fetch latest Audit record for Item
            var latestAuditRecord = await _unitOfWork.ItemRefAttriAuditRepository
                .GetFilteredAsync($"item_id = '{itemId}' AND source = '{source}' AND ApprovalStatus = 'PendingApproval'");

            var latestAuditRecordFilter = latestAuditRecord
                .OrderByDescending(x => x.ChangeTimestamp).FirstOrDefault();  // Get the most recent record


            // Return a new DTO mapping with latest data from both tables
            return new MDMItemRefAttriApprovalDTO
            {
                MainTable = itemRefAttributesFiltered,
                AuditTable = latestAuditRecordFilter
            };
        }

        public async Task<MDMItemRefAttriDTO> GetFilteredAsync(string itemId)
        {
            var itemRefAttributes = await _unitOfWork.ItemRefAttributesRepository
                                     .GetFilteredAsync($"item_id = '{itemId}'");

            var singleRecord = itemRefAttributes.FirstOrDefault();

            return _mapper.Map<MDMItemRefAttriDTO>(singleRecord);
        }

        public async Task<MDMItemRefAttriDTO> GetFilteredAsync(string itemId, string source)
        {

            var filterQuery = $"item_id = '{itemId}' AND source = '{source}'";

            var singleRecord = await _unitOfWork.ItemRefAttributesRepository.GetSingleFilteredAsync(filterQuery);


            return _mapper.Map<MDMItemRefAttriDTO>(singleRecord);
        }



        public async Task<IEnumerable<MDMItemRefAttributesAudit>> ItemAuditGetAsync()
        {
            var auditRecords = await _unitOfWork.ItemRefAttriAuditRepository.GetAllAsync();

            //Active records will in null or active
            var auditRecordsFilter = auditRecords.Where(x => x.ApprovalStatus.ToLower() == "pendingapproval").ToList();

            return _mapper.Map<IEnumerable<MDMItemRefAttributesAudit>>(auditRecordsFilter);

        }

        public async Task<MDMItemRefAttributesAudit> GetItemAuditAsync(Int64 auditId)
        {
            var itemAuditRefAttributes = await _unitOfWork.ItemRefAttriAuditRepository
                                     .GetFilteredAsync($"AuditId = '{auditId}'");

            var singleRecord = itemAuditRefAttributes.FirstOrDefault();

            return _mapper.Map<MDMItemRefAttributesAudit>(singleRecord);
        }


        public async Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string columnName)
        {

            var tableName = $"{_catalog}.mdm.item_reference_attributes";// "mdm_sandbox.mdm.item_reference_attributes"; // Replace with your actual table name
            var filter = "IsActive is not FALSE"; // Example filter: Active records only

            return await _unitOfWork.ItemRefAttributesRepository.GetDistinctColumnValuesAsync(tableName, columnName, filter);
        }

        public async Task<bool> UpdateReqByMakerAsync(string itemId, MDMItemRefAttriDTO updatedRecord, string makerId, string remarks)
        {
            try
            {
                var currentRecord = await _unitOfWork.ItemRefAttributesRepository.GetSingleFilteredAsync($"item_id = '{itemId}' AND source = '{updatedRecord.Source}'");
                if (currentRecord != null) await _columnEdit.KeepLockedValuesAsync(ColumnEditCatalog.Item, updatedRecord, currentRecord);
                // Add an entry to the audit table for the request
                var auditRecord = new MDMItemRefAttributesAudit
                {
                    Source = updatedRecord.Source,
                    ItemId = updatedRecord.ItemId,
                    ItemEnglishName = updatedRecord.ItemEnglishName,
                    CasNumber = updatedRecord.CasNumber,
                    ItemForm = updatedRecord.ItemForm,
                    Level3 = updatedRecord.Level3,
                    AddressableSpend = updatedRecord.AddressableSpend,
                    MaterialCriticalityBasedOnRevenue = updatedRecord.MaterialCriticalityBasedOnRevenue,
                    CurrentWavePlan = updatedRecord.CurrentWavePlan,

                    CreatedBy = updatedRecord.CreatedBy,
                    CreatedDate = updatedRecord.CreatedDate,
                    UpdatedBy = makerId,
                    UpdatedDate = DateTime.UtcNow,
                    IsActive = updatedRecord.IsActive,
                    ChangeType = "Update",
                    ApprovalStatus = "PendingApproval",
                    LastModifiedByRole = "Maker",
                    MakerId = makerId,
                    ChangeTimestamp = DateTime.UtcNow,
                    Remarks = remarks
                };

                var ItemRefAttributesAudit = _mapper.Map<MDMItemRefAttributesAudit>(auditRecord);
                await _unitOfWork.ItemRefAttriAuditRepository.AddAsync(ItemRefAttributesAudit);


                //  var mainTableRecord = await _unitOfWork.ItemRefAttributesRepository.
                // GetRecordByColumnValueAsync<MDMItemRefAttributes>($"{_catalog}.mdm.item_reference_attributes",
                //"item_id",
                // itemId);


                var filterQuery = $"item_id = '{itemId}' AND source = '{updatedRecord.Source}'";

                var mainTableRecord = await _unitOfWork.ItemRefAttributesRepository.GetSingleFilteredAsync(filterQuery);

                if (mainTableRecord != null)
                {
                    mainTableRecord.UpdatedBy = makerId;
                    mainTableRecord.UpdatedDate = DateTime.UtcNow;

                    mainTableRecord.UpdateStatus = "PendingApproval";
                    mainTableRecord.LastModifiedByRole = "Maker";

                    //  await _unitOfWork.ItemRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.ItemId, "item_id");
                    await _unitOfWork.ItemRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.ItemId, "item_id", "source", updatedRecord.Source);

                }
                else
                    throw new Exception("Error while updating Item Ref Attributes table!");


                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        public async Task<bool> ApproveOrRejectUpdateAsync(int auditId, bool isApproved, string checkerId, string remarks)
        {
            // Fetch the audit record
            //var auditRecord = await _unitOfWork.ItemRefAttriAuditRepository.
            //    GetRecordByColumnValueAsync<MDMItemRefAttributesAudit>($"{_catalog}.mdm.item_reference_attributes_audit",
            //   "AuditId",
            //    auditId);

            //if (auditRecord == null || auditRecord.ApprovalStatus != "PendingApproval")
            //    throw new Exception("Invalid audit record or already processed.");

            var auditRecord = await _unitOfWork.ItemRefAttriAuditRepository
                                .GetSingleFilteredAsync($"AuditId = '{auditId}'");

            //var auditRecord = itemRefAuditAttributes.FirstOrDefault();


            if (isApproved)
            {
                // Update the main table
                var updatedRecord = new MDMItemRefAttributes
                {
                    Source = auditRecord.Source,
                    ItemId = auditRecord.ItemId,
                    ItemEnglishName = auditRecord.ItemEnglishName,
                    CasNumber = auditRecord.CasNumber,
                    ItemForm = auditRecord.ItemForm,
                    Level3 = auditRecord.Level3,
                    AddressableSpend = auditRecord.AddressableSpend,
                    MaterialCriticalityBasedOnRevenue = auditRecord.MaterialCriticalityBasedOnRevenue,
                    CurrentWavePlan = auditRecord.CurrentWavePlan,

                    CreatedBy = auditRecord.CreatedBy,
                    CreatedDate = auditRecord.CreatedDate,
                    UpdatedBy = checkerId,
                    UpdatedDate = DateTime.UtcNow,
                    IsActive = auditRecord.IsActive,

                    UpdateStatus = "Approved",
                    LastModifiedByRole = "Checker",
                    Remarks = remarks
                };

                //Update Main table
                var currentRecord = await _unitOfWork.ItemRefAttributesRepository.GetSingleFilteredAsync($"item_id = '{auditRecord.ItemId}' AND source = '{auditRecord.Source}'");
                if (currentRecord != null) await _columnEdit.KeepLockedValuesAsync(ColumnEditCatalog.Item, updatedRecord, currentRecord);
                // await _unitOfWork.ItemRefAttributesRepository.UpdateAsync(updatedRecord, updatedRecord.ItemId, "item_id");
                await _unitOfWork.ItemRefAttributesRepository.UpdateAsync(updatedRecord, updatedRecord.ItemId, "item_id", "source", auditRecord.Source);

                // Update the audit record
                auditRecord.ApprovalStatus = "Approved";
            }
            else
            {
                // Rejected: keep the main table's current values, only record the checker's decision
                var mainRecord = await _unitOfWork.ItemRefAttributesRepository
                    .GetSingleFilteredAsync($"item_id = '{auditRecord.ItemId}' AND source = '{auditRecord.Source}'");

                mainRecord.UpdatedBy = checkerId;
                mainRecord.UpdatedDate = DateTime.UtcNow;
                mainRecord.UpdateStatus = "Rejected";
                mainRecord.LastModifiedByRole = "Checker";
                mainRecord.Remarks = remarks;

                //Update Main table
                await _unitOfWork.ItemRefAttributesRepository.UpdateAsync(mainRecord, mainRecord.ItemId, "item_id", "source", auditRecord.Source);

                // Update the audit record for rejection
                auditRecord.ApprovalStatus = "Rejected";
            }

            auditRecord.CheckerId = checkerId;
            auditRecord.ApprovalTimestamp = DateTime.UtcNow;
            auditRecord.Remarks = remarks;
            //Update Audit table
            await _unitOfWork.ItemRefAttriAuditRepository.UpdateAsync(auditRecord, auditRecord.AuditId, "AuditId"); ;
            return true;
        }

        public async Task<bool> UpdateDirectlyAsync(string itemId, MDMItemRefAttriDTO updatedRecord, bool isApproved, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {

                //  var mainTableRecord = await _unitOfWork.ItemRefAttributesRepository.
                // GetRecordByColumnValueAsync<MDMItemRefAttributes>($"{_catalog}.mdm.item_reference_attributes",
                //"item_id",
                // itemId);

                var filterQuery = $"item_id = '{itemId}' AND source = '{updatedRecord.Source}'";

                var mainTableRecord = await _unitOfWork.ItemRefAttributesRepository.GetSingleFilteredAsync(filterQuery);

                if (mainTableRecord != null)
                {

                    await _columnEdit.KeepLockedValuesAsync(ColumnEditCatalog.Item, updatedRecord, mainTableRecord);
                    mainTableRecord.ItemEnglishName = updatedRecord.ItemEnglishName;
                    mainTableRecord.CasNumber = updatedRecord.CasNumber;
                    mainTableRecord.ItemForm = updatedRecord.ItemForm;
                    mainTableRecord.Level3 = updatedRecord.Level3;
                    mainTableRecord.AddressableSpend = updatedRecord.AddressableSpend;
                    mainTableRecord.MaterialCriticalityBasedOnRevenue = updatedRecord.MaterialCriticalityBasedOnRevenue;
                    mainTableRecord.CurrentWavePlan = updatedRecord.CurrentWavePlan;


                    //mainTableRecord.CreatedBy = updatedRecord.CreatedBy; //should not update as it should not modify in main table
                    //mainTableRecord.CreatedDate = updatedRecord.CreatedDate; //should not update as it should not modify in main table
                    mainTableRecord.UpdatedBy = checkerId;
                    mainTableRecord.UpdatedDate = DateTime.UtcNow;
                    mainTableRecord.IsActive = true;  //auditRecord.IsActive;

                    mainTableRecord.UpdateStatus = "UpdateDirectly";
                    mainTableRecord.LastModifiedByRole = role;
                    mainTableRecord.Remarks = remarks;

                    //  await _unitOfWork.ItemRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.ItemId, "item_id");
                    await _unitOfWork.ItemRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.ItemId, "item_id", "source", updatedRecord.Source);




                }
                else
                    throw new Exception("Error while updating Item Ref Attributes table!");



                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public async Task BulkUpdateAsync(
      IEnumerable<MDMViewItemRefAttrsDTO> entities,
      string checkerId,
      string role,
      string? remarks = null)
        {
            try
            {
                //  STEP 1: Start Execution Timer
                //var stopwatch = Stopwatch.StartNew();

                //if (entities == null || !entities.Any())
                //    throw new ArgumentException("Entities cannot be null or empty", nameof(entities));

                //  STEP 2: Convert DTOs to Entities  
                var uiEntitiesToUpdate = entities
                    .Select(e => new MDMItemRefAttributes
                    {
                        ItemId = e.ItemId,
                        Source = e.Source,
                        AddressableSpend = e.AddressableSpend,
                        CurrentWavePlan = e.CurrentWavePlan,
                        CasNumber = e.CasNumber,
                        ItemEnglishName = e.ItemEnglishName,
                        ItemForm = e.ItemForm,
                        LastModifiedByRole = e.LastModifiedByRole,
                        Level3 = e.Level3,
                        MaterialCriticalityBasedOnRevenue = e.MaterialCriticalityBasedOnRevenue
                    })
                    .ToList();

                //  STEP 3: Fetch Existing Records from DB  
                var dbEntities = await _unitOfWork.ItemRefAttributesRepository
                                                  .GetDBRecordsAsync(uiEntitiesToUpdate, "item_id", "source", null);

                // Convert DB records to dictionary for fast lookup
                var dbEntitiesDict = dbEntities.ToDictionary(x => (x.ItemId, x.Source)); // Using Tuple as composite key

                //  STEP 4: Identify Records for Update
                var dbEntitiesToUpdate = new List<MDMItemRefAttributes>();

                foreach (var uiEntity in uiEntitiesToUpdate)
                {
                    if (dbEntitiesDict.TryGetValue((uiEntity.ItemId, uiEntity.Source), out var existingEntity))
                    {
                        // Update only if the record exists  
                        await _columnEdit.KeepLockedValuesAsync(ColumnEditCatalog.Item, uiEntity, existingEntity);
                        existingEntity.Level3 = uiEntity.Level3;
                        existingEntity.CurrentWavePlan = uiEntity.CurrentWavePlan;
                        existingEntity.AddressableSpend = uiEntity.AddressableSpend;
                        existingEntity.CasNumber = uiEntity.CasNumber;
                        existingEntity.ItemEnglishName = uiEntity.ItemEnglishName;
                        existingEntity.ItemForm = uiEntity.ItemForm;
                        existingEntity.MaterialCriticalityBasedOnRevenue = uiEntity.MaterialCriticalityBasedOnRevenue;

                        existingEntity.UpdatedBy = checkerId;
                        existingEntity.UpdatedDate = DateTime.UtcNow;
                        existingEntity.IsActive = true;
                        existingEntity.UpdateStatus = "UpdateDirectly";
                        existingEntity.LastModifiedByRole = role;
                        existingEntity.Remarks = remarks;

                        dbEntitiesToUpdate.Add(existingEntity);
                    }
                }

                //  STEP 5: Perform Bulk Update  
                if (dbEntitiesToUpdate.Any())
                {
                    await _unitOfWork.ItemRefAttributesRepository.BulkUpdateAsync(dbEntitiesToUpdate, "item_id", "source", null);
                }

                //  STEP 6: Stop Timer and Log Execution Time  
                //stopwatch.Stop();
                //Console.WriteLine($"BulkUpdateAsync executed in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }



    }
}
