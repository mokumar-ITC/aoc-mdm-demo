using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;


namespace AOC_MDM.Services.Service
{

    public class MDMItemSupplierRefAttriService : IMDMItemSupplierRefAttriService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMItemSupplierRefAttriService> _logger;
        private readonly string _catalog;

        public MDMItemSupplierRefAttriService(
            IUnitOfWork unitOfWork,
            IMapper mapper
             , ILogger<MDMItemSupplierRefAttriService> logger
         , DatabaseSettings dbSettings // Inject the catalog name via the settings object

     )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _catalog = dbSettings.Catalog; // Store the catalog name
        }

        public async Task<MDMItemSupplierRefAttriApprovalDTO> GetApprovalAsync(string itemId, string vendorId, string source)
        {
            MDMItemSupplierRefAttriApprovalDTO obj = new MDMItemSupplierRefAttriApprovalDTO();

            if (itemId == "0")
            {
                obj.AuditTable = new MDMItemSupplierRefAttributesAudit();
                obj.MainTable = new MDMItemSupplierRefAttributes();
                return obj;
            }


            //// Fetch main table attributes
            var filterQuery = $"item_id = '{itemId}' AND vendor_id = '{vendorId}' AND source = '{source}'";

            var ItemSupplierRefAttributesFiltered = await _unitOfWork.ItemSupplierRefAttributesRepository.GetSingleFilteredAsync(filterQuery);


            // If the Main table record isn't found, return null or handle as appropriate
            if (ItemSupplierRefAttributesFiltered == null)
            {
                return obj; // Or return default empty DTO based on your design preference.
            }

            // Fetch audit table attributes
            //// LINQ query to join main table with the latest audit record

            // Fetch latest Audit record for Item
            var latestAuditRecord = await _unitOfWork.ItemSupplierRefAttriAuditRepository
                .GetFilteredAsync($"item_id = '{itemId}'  AND vendor_id = '{vendorId}' AND source = '{source}' AND ApprovalStatus = 'PendingApproval'");

            var latestAuditRecordFilter = latestAuditRecord
                .OrderByDescending(x => x.ChangeTimestamp).FirstOrDefault();  // Get the most recent record


            // Return a new DTO mapping with latest data from both tables
            return new MDMItemSupplierRefAttriApprovalDTO
            {
                MainTable = ItemSupplierRefAttributesFiltered,
                AuditTable = latestAuditRecordFilter
            };


        }


        public async Task<MDMItemSupplierRefAttriDTO> GetFilteredAsync(string id)
        {
            var itemSupplierRefAttributes = await _unitOfWork.ItemSupplierRefAttributesRepository
                                     .GetFilteredAsync($"item_id = '{id}'");

            var singleRecord = itemSupplierRefAttributes.FirstOrDefault();

            return _mapper.Map<MDMItemSupplierRefAttriDTO>(singleRecord);
        }



        public async Task<MDMItemSupplierRefAttriDTO> GetFilteredAsync(string itemId, string vendorId, string source)
        {

            var filterQuery = $"item_id = '{itemId}' AND vendor_id = '{vendorId}' AND source = '{source}'";

            var singleRecord = await _unitOfWork.ItemSupplierRefAttributesRepository.GetSingleFilteredAsync(filterQuery);

            return _mapper.Map<MDMItemSupplierRefAttriDTO>(singleRecord);
        }



        public async Task<IEnumerable<MDMItemSupplierRefAttributesAudit>> ItemSupplierAuditGetAsync()
        {
            var auditRecords = await _unitOfWork.ItemSupplierRefAttriAuditRepository.GetAllAsync();

            //Active records will in null or active
            var auditRecordsFilter = auditRecords.Where(x => x.ApprovalStatus.ToLower() == "pendingapproval").ToList();

            return _mapper.Map<IEnumerable<MDMItemSupplierRefAttributesAudit>>(auditRecordsFilter);

        }
        public async Task<IEnumerable<MDMItemSupplierRefAttributesAudit>> ItemSupplierAuditGetAsync(Int64 id)
        {
            var itemSupplierRefAuditAttributes = await _unitOfWork.ItemSupplierRefAttriAuditRepository
                                   .GetFilteredAsync($"AuditId = '{id}'");

            return _mapper.Map<IEnumerable<MDMItemSupplierRefAttributesAudit>>(itemSupplierRefAuditAttributes);
        }

        public async Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string columnName)
        {

            var tableName = $"{_catalog}.mdm.item_supplier_reference_attributes";// "mdm_sandbox.mdm.item_supplier_reference_attributes"; // Replace with your actual table name
            var filter = "IsActive is not FALSE"; // Example filter: Active records only

            return await _unitOfWork.ItemSupplierRefAttributesRepository.GetDistinctColumnValuesAsync(tableName, columnName, filter);
        }

        public async Task<bool> UpdateReqByMakerAsync(string itemId, MDMItemSupplierRefAttriDTO updatedRecord, string makerId, string remarks)
        {
            try
            {
                // Add an entry to the audit table for the request
                var auditRecord = new MDMItemSupplierRefAttributesAudit
                {
                    Source = updatedRecord.Source,
                    ItemId = updatedRecord.ItemId,
                    VendorId = updatedRecord.VendorId,
                    RiskTransfer = updatedRecord.RiskTransfer,
                    SupplierSpecificRequirements = updatedRecord.SupplierSpecificRequirements,
                    FormulaNonFormula = updatedRecord.FormulaNonFormula,
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

                var ItemSupplierRefAttributesAudit = _mapper.Map<MDMItemSupplierRefAttributesAudit>(auditRecord);
                await _unitOfWork.ItemSupplierRefAttriAuditRepository.AddAsync(ItemSupplierRefAttributesAudit);

                var filterQuery = $"item_id = '{itemId}' AND vendor_id = '{updatedRecord.VendorId}' AND source = '{updatedRecord.Source}'";

                var mainTableRecord = await _unitOfWork.ItemSupplierRefAttributesRepository.GetSingleFilteredAsync(filterQuery);



                if (mainTableRecord != null)
                {
                    mainTableRecord.UpdatedBy = makerId;
                    mainTableRecord.UpdatedDate = DateTime.UtcNow;

                    mainTableRecord.UpdateStatus = "PendingApproval";
                    mainTableRecord.LastModifiedByRole = "Maker";

                    await _unitOfWork.ItemSupplierRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.ItemId, "item_id", "source",
                        updatedRecord.Source, "vendor_id", updatedRecord.VendorId);

                }
                else
                    throw new Exception("Error while updating ItemSupplier Ref Attributes table!");


                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        public async Task<bool> UpdateDirectlyAsync(string itemId, MDMItemSupplierRefAttriDTO updatedRecord, bool isApproved, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                var filterQuery = $"item_id = '{itemId}' AND vendor_id = '{updatedRecord.VendorId}' AND source = '{updatedRecord.Source}'";

                var mainTableRecord = await _unitOfWork.ItemSupplierRefAttributesRepository.GetSingleFilteredAsync(filterQuery);


                if (mainTableRecord != null)
                {

                    mainTableRecord.Source = updatedRecord.Source;
                    mainTableRecord.ItemId = updatedRecord.ItemId;
                    mainTableRecord.VendorId = updatedRecord.VendorId;
                    mainTableRecord.RiskTransfer = updatedRecord.RiskTransfer;
                    mainTableRecord.SupplierSpecificRequirements = updatedRecord.SupplierSpecificRequirements;
                    mainTableRecord.FormulaNonFormula = updatedRecord.FormulaNonFormula;

                    //mainTableRecord.CreatedBy = updatedRecord.CreatedBy; //should not update as it should not modify in main table
                    //mainTableRecord.CreatedDate = updatedRecord.CreatedDate; //should not update as it should not modify in main table
                    mainTableRecord.UpdatedBy = checkerId;
                    mainTableRecord.UpdatedDate = DateTime.UtcNow;
                    mainTableRecord.IsActive = true;  //auditRecord.IsActive;

                    mainTableRecord.UpdateStatus = "UpdateDirectly";
                    mainTableRecord.LastModifiedByRole = role;
                    mainTableRecord.Remarks = remarks;

                    // await _unitOfWork.ItemSupplierRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.ItemId, "item_id");

                    await _unitOfWork.ItemSupplierRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.ItemId, "item_id", "source",
                        updatedRecord.Source, "vendor_id", updatedRecord.VendorId);

                }
                else
                    throw new Exception("Error while updating Item Supplier Ref Attributes table!");

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public async Task BulkUpdateAsync(IEnumerable<MDMViewItemSupplierRefAttrsDTO> entities, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                //  STEP 2: Convert DTOs to Entities  
                var uiEntitiesToUpdate = entities
                    .Select(e => new MDMItemSupplierRefAttributes
                    {
                        ItemId = e.ItemId,
                        VendorId = e.VendorId,
                        Source = e.Source,
                        RiskTransfer = e.RiskTransfer,
                        SupplierSpecificRequirements = e.SupplierSpecificRequirements,
                        FormulaNonFormula = e.FormulaNonFormula,

                    })
                    .ToList();

                //  STEP 3: Fetch Existing Records from DB  
                var dbEntities = await _unitOfWork.ItemSupplierRefAttributesRepository
                                                  .GetDBRecordsAsync(uiEntitiesToUpdate, "item_id", "source", "vendor_id");

                // Convert DB records to dictionary for fast lookup
                var dbEntitiesDict = dbEntities.ToDictionary(x => (x.ItemId, x.VendorId, x.Source)); // Using Tuple as composite key

                //  STEP 4: Identify Records for Update
                var dbEntitiesToUpdate = new List<MDMItemSupplierRefAttributes>();

                foreach (var uiEntity in uiEntitiesToUpdate)
                {
                    if (dbEntitiesDict.TryGetValue((uiEntity.ItemId, uiEntity.VendorId, uiEntity.Source), out var existingEntity))
                    {
                        // Update only if the record exists  
                        //existingEntity.Source = uiEntity.Source;
                        //existingEntity.VendorId = uiEntity.VendorId;
                        existingEntity.RiskTransfer = uiEntity.RiskTransfer;
                        existingEntity.SupplierSpecificRequirements = uiEntity.SupplierSpecificRequirements;
                        existingEntity.FormulaNonFormula = uiEntity.FormulaNonFormula;

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
                    await _unitOfWork.ItemSupplierRefAttributesRepository.BulkUpdateAsync(dbEntitiesToUpdate, "item_id", "vendor_id", "source");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BulkUpdateAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> ApproveOrRejectUpdateAsync(int auditId, bool isApproved, string checkerId, string remarks)
        {
            // Fetch the audit record
            var auditRecord = await _unitOfWork.ItemSupplierRefAttriAuditRepository
                         .GetSingleFilteredAsync($"AuditId = '{auditId}'");

            if (isApproved)
            {
                // Update the main table
                var updatedRecord = new MDMItemSupplierRefAttributes
                {
                    Source = auditRecord.Source,
                    ItemId = auditRecord.ItemId,
                    VendorId = auditRecord.VendorId,
                    RiskTransfer = auditRecord.RiskTransfer,
                    SupplierSpecificRequirements = auditRecord.SupplierSpecificRequirements,
                    FormulaNonFormula = auditRecord.FormulaNonFormula,
                    CreatedBy = auditRecord.CreatedBy,
                    CreatedDate = auditRecord.CreatedDate,
                    UpdatedBy = checkerId,
                    UpdatedDate = DateTime.UtcNow,
                    IsActive = true,  //auditRecord.IsActive,

                    UpdateStatus = "Approved",
                    LastModifiedByRole = "Checker",
                    Remarks = remarks
                };
                // await _unitOfWork.ItemSupplierRefAttributesRepository.UpdateAsync(updatedRecord, updatedRecord.ItemId, "item_id");
                await _unitOfWork.ItemSupplierRefAttributesRepository.UpdateAsync(updatedRecord, updatedRecord.ItemId, "item_id", "source",
                        auditRecord.Source, "vendor_id", auditRecord.VendorId);

                // Update the audit record
                auditRecord.ApprovalStatus = "Approved";
            }
            else
            {
                // Rejected: keep the main table's current values, only record the checker's decision
                var mainRecord = await _unitOfWork.ItemSupplierRefAttributesRepository
                    .GetSingleFilteredAsync($"item_id = '{auditRecord.ItemId}' AND source = '{auditRecord.Source}' AND vendor_id = '{auditRecord.VendorId}'");

                mainRecord.UpdatedBy = checkerId;
                mainRecord.UpdatedDate = DateTime.UtcNow;
                mainRecord.UpdateStatus = "Rejected";
                mainRecord.LastModifiedByRole = "Checker";
                mainRecord.Remarks = remarks;

                await _unitOfWork.ItemSupplierRefAttributesRepository.UpdateAsync(mainRecord, mainRecord.ItemId, "item_id", "source",
                        auditRecord.Source, "vendor_id", auditRecord.VendorId);


                // Update the audit record for rejection
                auditRecord.ApprovalStatus = "Rejected";
            }

            auditRecord.CheckerId = checkerId;
            auditRecord.ApprovalTimestamp = DateTime.UtcNow;
            auditRecord.Remarks = remarks;
            auditRecord.IsActive = true;

            await _unitOfWork.ItemSupplierRefAttriAuditRepository.UpdateAsync(auditRecord, auditRecord.AuditId, "AuditId"); ;
            return true;
        }
    }
}
