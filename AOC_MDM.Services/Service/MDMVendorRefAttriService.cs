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


namespace AOC_MDM.Services.Service
{

    public class MDMVendorRefAttriService : IMDMVendorRefAttriService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MDMVendorRefAttriService> _logger;
        private readonly string _catalog;

        public MDMVendorRefAttriService(
            IUnitOfWork unitOfWork,
            IMapper mapper
           , ILogger<MDMVendorRefAttriService> logger
           , DatabaseSettings dbSettings // Inject the catalog name via the settings object

     )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _catalog = dbSettings.Catalog; // Store the catalog name
        }

        /// <summary>
        /// Get record based on Vendor Id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<MDMVendorRefAttriDTO> GetFilteredAsync(string Id)
        {
            var vendorRefAttributes = await _unitOfWork.VendorRefAttributesRepository
                                     .GetFilteredAsync($"vendor_id = '{Id}'");

            var singleRecord = vendorRefAttributes.FirstOrDefault();

            return _mapper.Map<MDMVendorRefAttriDTO>(singleRecord);
        }


        public async Task<MDMVendorRefAttriDTO> GetFilteredAsync(string id, string source)
        {

            var filterQuery = $"vendor_id = '{id}' AND source = '{source}'";

            var singleRecord = await _unitOfWork.VendorRefAttributesRepository.GetSingleFilteredAsync(filterQuery);


            return _mapper.Map<MDMVendorRefAttriDTO>(singleRecord);
        }



        public async Task<MDMVendorRefAttriApprovalDTO> GetApprovalAsync(string vendorId, string source)
        {
            MDMVendorRefAttriApprovalDTO obj = new MDMVendorRefAttriApprovalDTO();

            if (vendorId == "0")
            {
                obj.AuditTable = new MDMVendorRefAttributesAudit();
                obj.MainTable = new MDMVendorRefAttributes();
                return obj;
            }


            // Fetch main table attributes
            // var mainTable = await _unitOfWork.VendorRefAttributesRepository
            //                      .GetFilteredAsync($"vendor_id = '{vendorId}'"); 

            //var mainTableFiltered = mainTable.FirstOrDefault();


            var filterQuery = $"vendor_id = '{vendorId}' AND source = '{source}'";

            var mainTableFiltered = await _unitOfWork.VendorRefAttributesRepository.GetSingleFilteredAsync(filterQuery);



            // If the Main table record isn't found, return null or handle as appropriate
            if (mainTableFiltered == null)
            {
                return obj; // Or return default empty DTO based on your design preference.
            }


            // Fetch audit table attributes
            //var auditTableAttributes = await _unitOfWork.VendorRefAttriAuditRepository.GetAllAsync();

            //// Filter active records for the audit table Note/TODO: x.IsActive != false (for timebeing removed, if required, we add back)
            //var auditTableAttributesFiltered = auditTableAttributes
            //    .Where(x => x.VendorId == vendorId && x.ApprovalStatus == "PendingApproval")
            //    .ToList();

            //// LINQ query to join main table with the latest audit record
            //var result = (from main in vendorRefAttributesFiltered
            //              join audit in
            //                  (from a in auditTableAttributesFiltered
            //                   group a by a.VendorId into auditGroup
            //                   let latestRecord = auditGroup
            //                       .OrderByDescending(a => a.ChangeTimestamp)
            //                       .FirstOrDefault()
            //                   select latestRecord)
            //              on main.VendorId equals audit.VendorId
            //              select new MDMVendorRefAttriApprovalDTO
            //              {
            //                  MainTable = main,
            //                  AuditTable = audit
            //              })
            //             .FirstOrDefault(); // Take only the first match, as we are returning a single DTO.

            //// Return the result
            //return result;


            // Fetch latest Audit record for VendorId
            var latestAuditRecord = await _unitOfWork.VendorRefAttriAuditRepository
                .GetFilteredAsync($"vendor_id = '{vendorId}' AND source = '{source}' AND ApprovalStatus = 'PendingApproval'");

            var latestAuditRecordFilter = latestAuditRecord
                .OrderByDescending(x => x.ChangeTimestamp).FirstOrDefault();  // Get the most recent record


            // Return a new DTO mapping with latest data from both tables
            return new MDMVendorRefAttriApprovalDTO
            {
                MainTable = mainTableFiltered,
                AuditTable = latestAuditRecordFilter
            };
        }

        public async Task<IEnumerable<MDMVendorRefAttributesAudit>> VendorAuditGetAsync()
        {
            var auditRecords = await _unitOfWork.VendorRefAttriAuditRepository.GetAllAsync();

            //Active records will in null or active
            var auditRecordsFilter = auditRecords.Where(x => x.ApprovalStatus.ToLower() == "pendingapproval").ToList();

            return _mapper.Map<IEnumerable<MDMVendorRefAttributesAudit>>(auditRecordsFilter);

        }


        public async Task<MDMVendorRefAttributesAudit> VendorAuditGetAsync(Int64 id)
        {
            var vendorRefAuditAttributes = await _unitOfWork.VendorRefAttriAuditRepository
                                   .GetFilteredAsync($"AuditId = '{id}'");

            var singleRecord = vendorRefAuditAttributes.FirstOrDefault();

            return _mapper.Map<MDMVendorRefAttributesAudit>(singleRecord);
        }

        public async Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string columnName)
        {

            var tableName = $"{_catalog}.mdm.vendor_reference_attributes";// "mdm_sandbox.mdm.vendor_reference_attributes"; // Replace with your actual table name
            var filter = "IsActive is not FALSE"; // Example filter: Active records only

            return await _unitOfWork.VendorRefAttributesRepository.GetDistinctColumnValuesAsync(tableName, columnName, filter);
        }


        public async Task<bool> AddAsync(MDMVendorRefAttriDTO mdmVendorRefAttriDTO, string makerId)
        {
            try
            {

                MDMVendorRefAttributesAudit audit = new MDMVendorRefAttributesAudit();

                // Enforce default values for some columns if not provided
                audit.ChangeType = "Insert";
                audit.ApprovalStatus = "Pending";
                audit.MakerId = makerId;
                audit.IsActive = true;
                audit.Source = "sql_db";//mdmVendorRefAttriDTO.Source;
                audit.VendorId = mdmVendorRefAttriDTO.VendorId;
                audit.SupplierVendor = mdmVendorRefAttriDTO.SupplierVendor;
                audit.AddressableSpend = mdmVendorRefAttriDTO.VendorId;
                audit.Intercompany = mdmVendorRefAttriDTO.Intercompany;

                audit.IndirectSpendOwner = mdmVendorRefAttriDTO.IndirectSpendOwner;
                audit.Level3 = mdmVendorRefAttriDTO.Level3;

                audit.ParentCompany = mdmVendorRefAttriDTO.ParentCompany;
                audit.PayablesVeryImportantSupplier = mdmVendorRefAttriDTO.PayablesVeryImportantSupplier;
                audit.VendorEnglishName = mdmVendorRefAttriDTO.VendorEnglishName;

                audit.VendorFamilyName = mdmVendorRefAttriDTO.VendorFamilyName;

                audit.CreatedBy = makerId;
                audit.UpdatedBy = null;
                audit.CreatedDate = DateTime.UtcNow;
                audit.UpdatedDate = null;
                audit.ApprovalTimestamp = null;
                audit.ChangeTimestamp = DateTime.UtcNow;

                var vendorRefAttributes = _mapper.Map<MDMVendorRefAttributesAudit>(audit);
                await _unitOfWork.VendorRefAttriAuditRepository.AddAsync(vendorRefAttributes);
                // _logger.LogInformation("Successfully added SpendArea entity with ID: {Id}", spendArea.Id);
                return true;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error occurred while adding SpendArea entity.");
                throw;
            }
        }

        public async Task<bool> AddRecordInMainTableAsync(Int64 auditId, string checkerId, string remarks, string status)
        {
            //Get Audit table data based on Audit Id
            var auditRecords = await _unitOfWork.VendorRefAttriAuditRepository.GetAllAsync();

            // Filter records to exclude the specified AuditId
            var filteredAuditRecords = auditRecords.Where(x => x.AuditId == auditId);

            // Retrieve the latest record based on ChangeTimestamp (or your desired field)
            var latestAuditRecord = filteredAuditRecords
               .OrderByDescending(x => x.ChangeTimestamp ?? DateTime.MinValue)// Replace ChangeTimestamp with your desired field
                .FirstOrDefault();

            try
            {

                if (status == "Approved")
                {
                    MDMVendorRefAttributes mainTable = new MDMVendorRefAttributes();

                    mainTable.IsActive = true;
                    mainTable.Source = latestAuditRecord.Source;//mdmVendorRefAttriDTO.Source;
                    mainTable.VendorId = latestAuditRecord.VendorId;
                    mainTable.SupplierVendor = latestAuditRecord.SupplierVendor;
                    mainTable.AddressableSpend = latestAuditRecord.VendorId;
                    mainTable.Intercompany = latestAuditRecord.Intercompany;

                    mainTable.IndirectSpendOwner = latestAuditRecord.IndirectSpendOwner;
                    mainTable.Level3 = latestAuditRecord.Level3;

                    mainTable.ParentCompany = latestAuditRecord.ParentCompany;
                    mainTable.PayablesVeryImportantSupplier = latestAuditRecord.PayablesVeryImportantSupplier;
                    mainTable.VendorEnglishName = latestAuditRecord.VendorEnglishName;

                    mainTable.VendorFamilyName = latestAuditRecord.VendorFamilyName;


                    //string formattedDate = DateTime.Parse("23-11-2024 15:20:16").ToString("yyyy-MM-dd HH:mm:ss");

                    mainTable.CreatedBy = latestAuditRecord.MakerId;
                    // audit.CreatedDate = DateTime.Parse( DateTime.Parse(DateTime.UtcNow.ToString()).ToString("yyyy-MM-dd HH:mm:ss")); 
                    mainTable.UpdatedBy = null;
                    //   audit.UpdatedDate = null;
                    mainTable.CreatedDate = DateTime.UtcNow;
                    mainTable.UpdatedDate = null;


                    var vendorRefAttributes = _mapper.Map<MDMVendorRefAttributes>(mainTable);
                    await _unitOfWork.VendorRefAttributesRepository.AddAsync(vendorRefAttributes);

                }

                latestAuditRecord.ApprovalStatus = status; //"Approved";
                latestAuditRecord.ApprovalTimestamp = DateTime.UtcNow;
                latestAuditRecord.CheckerId = checkerId;  //login id
                latestAuditRecord.Remarks = remarks;

                await _unitOfWork.VendorRefAttriAuditRepository.UpdateAsync(latestAuditRecord, latestAuditRecord.AuditId, "AuditId");

                // _logger.LogInformation("Successfully added SpendArea entity with ID: {Id}", spendArea.Id);
                return true;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error occurred while adding SpendArea entity.");
                throw;
            }
        }


        public async Task<bool> UpdateReqByMakerAsync(string vendorId, MDMVendorRefAttriDTO updatedRecord, string makerId, string remarks)
        {
            try
            {
                // Add an entry to the audit table for the request
                var auditRecord = new MDMVendorRefAttributesAudit
                {
                    // AuditId = Guid.NewGuid().ToString(),
                    Source = updatedRecord.Source,
                    VendorId = updatedRecord.VendorId,
                    SupplierVendor = updatedRecord.SupplierVendor,
                    VendorEnglishName = updatedRecord.VendorEnglishName,
                    VendorFamilyName = updatedRecord.VendorFamilyName,
                    Level3 = updatedRecord.Level3,
                    ParentCompany = updatedRecord.ParentCompany,
                    AddressableSpend = updatedRecord.AddressableSpend,
                    PayablesVeryImportantSupplier = updatedRecord.PayablesVeryImportantSupplier,
                    Intercompany = updatedRecord.Intercompany,
                    IndirectSpendOwner = updatedRecord.IndirectSpendOwner,
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


                // Kick off both tasks in parallel
                var addAuditTask = _unitOfWork.VendorRefAttriAuditRepository.AddAsync(auditRecord);

                //var fetchMainTableRecordTask = _unitOfWork.VendorRefAttributesRepository
                //    .GetRecordByColumnValueAsync<MDMVendorRefAttributes>(
                //        $"{_catalog}.mdm.vendor_reference_attributes",
                //        "vendor_id",
                //        vendorId);

                var filterQuery = $"vendor_id = '{vendorId}' AND source = '{updatedRecord.Source}'";

                var fetchMainTableRecordTask = _unitOfWork.VendorRefAttributesRepository.GetSingleFilteredAsync(filterQuery);

                // Wait for both tasks to complete
                await Task.WhenAll(addAuditTask, fetchMainTableRecordTask);

                // Get the result of the fetch task
                var mainTableRecord = fetchMainTableRecordTask.Result;

                if (mainTableRecord == null)
                {
                    throw new InvalidOperationException("Vendor record not found.");
                }


                //update main table
                mainTableRecord.UpdatedBy = makerId;
                mainTableRecord.UpdatedDate = DateTime.UtcNow;

                mainTableRecord.UpdateStatus = "PendingApproval";
                mainTableRecord.LastModifiedByRole = "Maker";

                // await _unitOfWork.VendorRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.VendorId, "vendor_id");
                await _unitOfWork.VendorRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.VendorId, "vendor_id", "source", updatedRecord.Source);


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
            //var vendorRefAuditAttributes = await _unitOfWork.VendorRefAttriAuditRepository
            //                       .GetFilteredAsync($"AuditId = '{auditId}'");

            //var auditRecord = vendorRefAuditAttributes.FirstOrDefault();

            var auditRecord = await _unitOfWork.VendorRefAttriAuditRepository
                                 .GetSingleFilteredAsync($"AuditId = '{auditId}'");

            // var auditRecord = vendorRefAuditAttributes.FirstOrDefault();

            if (isApproved)
            {
                // Update the main table
                var updatedRecord = new MDMVendorRefAttributes
                {
                    Source = auditRecord.Source,
                    VendorId = auditRecord.VendorId,
                    SupplierVendor = auditRecord.SupplierVendor,
                    VendorEnglishName = auditRecord.VendorEnglishName,
                    VendorFamilyName = auditRecord.VendorFamilyName,
                    Level3 = auditRecord.Level3,
                    ParentCompany = auditRecord.ParentCompany,
                    AddressableSpend = auditRecord.AddressableSpend,
                    PayablesVeryImportantSupplier = auditRecord.PayablesVeryImportantSupplier,
                    Intercompany = auditRecord.Intercompany,
                    IndirectSpendOwner = auditRecord.IndirectSpendOwner,
                    CreatedBy = auditRecord.CreatedBy,
                    CreatedDate = auditRecord.CreatedDate,
                    UpdatedBy = checkerId,
                    UpdatedDate = DateTime.UtcNow,
                    IsActive = true,  //auditRecord.IsActive,

                    UpdateStatus = "Approved",
                    LastModifiedByRole = "Checker",
                    Remarks = remarks
                };
                //  await _unitOfWork.VendorRefAttributesRepository.UpdateAsync(updatedRecord, updatedRecord.VendorId, "vendor_id");
                await _unitOfWork.VendorRefAttributesRepository.UpdateAsync(updatedRecord, updatedRecord.VendorId, "vendor_id", "source", updatedRecord.Source);

                // Update the audit record
                auditRecord.ApprovalStatus = "Approved";
            }
            else
            {
                // Rejected: keep the main table's current values, only record the checker's decision
                var mainRecord = await _unitOfWork.VendorRefAttributesRepository
                    .GetSingleFilteredAsync($"vendor_id = '{auditRecord.VendorId}' AND source = '{auditRecord.Source}'");

                mainRecord.UpdatedBy = checkerId;
                mainRecord.UpdatedDate = DateTime.UtcNow;
                mainRecord.UpdateStatus = "Rejected";
                mainRecord.LastModifiedByRole = "Checker";
                mainRecord.Remarks = remarks;

                await _unitOfWork.VendorRefAttributesRepository.UpdateAsync(mainRecord, mainRecord.VendorId, "vendor_id", "source", mainRecord.Source);

                // Update the audit record for rejection
                auditRecord.ApprovalStatus = "Rejected";
            }

            auditRecord.CheckerId = checkerId;
            auditRecord.ApprovalTimestamp = DateTime.UtcNow;
            auditRecord.Remarks = remarks;
            auditRecord.IsActive = true;

            await _unitOfWork.VendorRefAttriAuditRepository.UpdateAsync(auditRecord, auditRecord.AuditId, "AuditId");
            return true;
        }

        //No two step process - user update directly evern attributes tables if user has approver or above level 
        public async Task<bool> UpdateDirectlyAsync(string vendorId, MDMVendorRefAttriDTO updatedRecord, bool isApproved, string checkerId, string role, string? remarks = null)
        {
            //update the main table directly without going to audit table based on user level as approver or above
            try
            {
                //                var mainTableRecord = await _unitOfWork.VendorRefAttributesRepository.
                // GetRecordByColumnValueAsync<MDMVendorRefAttributes>($"{_catalog}.mdm.vendor_reference_attributes",
                //"vendor_id",
                // vendorId);

                var filterQuery = $"vendor_id = '{vendorId}' AND source = '{updatedRecord.Source}'";

                var mainTableRecord = await _unitOfWork.VendorRefAttributesRepository.GetSingleFilteredAsync(filterQuery);

                if (mainTableRecord != null)
                {
                    mainTableRecord.SupplierVendor = updatedRecord.SupplierVendor;
                    mainTableRecord.VendorEnglishName = updatedRecord.VendorEnglishName;
                    mainTableRecord.VendorFamilyName = updatedRecord.VendorFamilyName;
                    mainTableRecord.Level3 = updatedRecord.Level3;
                    mainTableRecord.ParentCompany = updatedRecord.ParentCompany;
                    mainTableRecord.AddressableSpend = updatedRecord.AddressableSpend;
                    mainTableRecord.PayablesVeryImportantSupplier = updatedRecord.PayablesVeryImportantSupplier;
                    mainTableRecord.Intercompany = updatedRecord.Intercompany;
                    mainTableRecord.IndirectSpendOwner = updatedRecord.IndirectSpendOwner;
                    //mainTableRecord.CreatedBy = updatedRecord.CreatedBy; //should not update as it should not modify in main table
                    //mainTableRecord.CreatedDate = updatedRecord.CreatedDate; //should not update as it should not modify in main table
                    mainTableRecord.UpdatedBy = checkerId;
                    mainTableRecord.UpdatedDate = DateTime.UtcNow;
                    mainTableRecord.IsActive = true;  //auditRecord.IsActive;

                    mainTableRecord.UpdateStatus = "UpdateDirectly";
                    mainTableRecord.LastModifiedByRole = role;
                    mainTableRecord.Remarks = remarks;

                    await _unitOfWork.VendorRefAttributesRepository.UpdateAsync(mainTableRecord, updatedRecord.VendorId, "vendor_id", "source", updatedRecord.Source);

                }
                else
                    throw new Exception("Error while updating Vendor Ref Attributes table!");


                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }
        public async Task BulkUpdateAsync(IEnumerable<MDMViewVendorRefAttrsDTO> entities,
                                        string checkerId, string role, string? remarks = null)
        {
            try
            {
                //  STEP 1: Start Execution Timer
                //var stopwatch = Stopwatch.StartNew();

                //if (entities == null || !entities.Any())
                //    throw new ArgumentException("Entities cannot be null or empty", nameof(entities));

                //  STEP 2: Convert DTOs to Entities  
                var uiEntitiesToUpdate = entities
                    .Select(e => new MDMVendorRefAttributes
                    {
                        VendorId = e.VendorId,
                        Source = e.Source,
                        AddressableSpend = e.AddressableSpend,
                        Level3 = e.Level3,
                        VendorEnglishName = e.VendorEnglishName,
                        VendorFamilyName = e.VendorFamilyName,
                        ParentCompany = e.ParentCompany,
                        PayablesVeryImportantSupplier = e.PayablesVeryImportantSupplier,
                        Intercompany = e.Intercompany,
                        IndirectSpendOwner = e.IndirectSpendOwner,
                        SupplierVendor = e.SupplierVendor

                    })
                    .ToList();

                //  STEP 3: Fetch Existing Records from DB  
                var dbEntities = await _unitOfWork.VendorRefAttributesRepository
                                                  .GetDBRecordsAsync(uiEntitiesToUpdate, "vendor_id", "source", null);

                // Convert DB records to dictionary for fast lookup
                var dbEntitiesDict = dbEntities.ToDictionary(x => (x.VendorId, x.Source)); // Using Tuple as composite key

                //  STEP 4: Identify Records for Update
                var dbEntitiesToUpdate = new List<MDMVendorRefAttributes>();

                foreach (var uiEntity in uiEntitiesToUpdate)
                {
                    if (dbEntitiesDict.TryGetValue((uiEntity.VendorId, uiEntity.Source), out var existingEntity))
                    {
                        // Update only if the record exists  
                        existingEntity.Level3 = uiEntity.Level3;
                        existingEntity.AddressableSpend = uiEntity.AddressableSpend;
                        existingEntity.VendorEnglishName = uiEntity.VendorEnglishName;
                        existingEntity.VendorFamilyName = uiEntity.VendorFamilyName;
                        existingEntity.ParentCompany = uiEntity.ParentCompany;
                        existingEntity.PayablesVeryImportantSupplier = uiEntity.PayablesVeryImportantSupplier;
                        existingEntity.Intercompany = uiEntity.Intercompany;
                        existingEntity.IndirectSpendOwner = uiEntity.IndirectSpendOwner;
                        existingEntity.SupplierVendor = uiEntity.SupplierVendor;

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
                    await _unitOfWork.VendorRefAttributesRepository.BulkUpdateAsync(dbEntitiesToUpdate, "vendor_id", "source", null);
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
