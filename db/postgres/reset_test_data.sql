-- =====================================================================
-- AOC MDM - wipe all data in aoc_mdm (keeps tables/views, keeps intranet.user_access).
-- Re-run 002_seed_test_data.sql afterwards to reload the test data.
-- DEV/TEST ONLY.
-- =====================================================================
TRUNCATE mdm.vendor_reference_attributes_audit,
         mdm.item_reference_attributes_audit,
         mdm.item_supplier_reference_attributes_audit
         RESTART IDENTITY;

TRUNCATE mdm.hierarchy_audit RESTART IDENTITY;

TRUNCATE mdm.column_edit_setting;

TRUNCATE mdm.vendor_reference_attributes,
         mdm.item_reference_attributes,
         mdm.item_supplier_reference_attributes,
         mdm.erp_vendor,
         mdm.erp_item,
         mdm.aoc_level_3,
         mdm.aoc_level_2,
         mdm.aoc_level_1,
         mdm.team,
         mdm.spend_area;
