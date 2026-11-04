-- =====================================================================
-- AOC MDM - Master Data Publish (BLITZDG step 14)
-- Read-only subscription views for downstream systems (CRM, ERP, lakehouse)
-- via ETL or OData. Only active records; pending maker requests are not
-- visible here until a checker approves them (the main tables only change
-- on approval or a direct update).
-- Idempotent.
-- =====================================================================

CREATE SCHEMA IF NOT EXISTS publish;

-- Full vendor hierarchy: Spend Area -> Team -> Level 1 -> Level 2 -> Level 3 (enabled only)
CREATE OR REPLACE VIEW publish.vendor_hierarchy AS
SELECT h.spend_area, h.team, h.level_1, h.level_2, h.level_3,
       h.feedstock_1, h.feedstock_2, h.feedstock_3,
       h.hazardous, h.conflict_mineral_forced_labor_reg_check
FROM mdm.vw_aoc_level_3 h
WHERE h.enabled = 'Yes' AND h.isactive IS NOT FALSE;

CREATE OR REPLACE VIEW publish.vendor_master AS
SELECT v.source, v.vendor_id, v.vendor_number, v.vendor_name,
       v.vendor_english_name, v.vendor_family_name, v.supplier_vendor, v.parent_company,
       v.level_3, v.mdm_level_2 AS level_2, v.mdm_level_1 AS level_1, v.team, v.spend_area,
       v.addressable_spend, v.payables_very_important_supplier, v.intercompany, v.indirect_spend_owner,
       CASE WHEN COALESCE(v.level_3, '') = '' THEN 'Unmapped' ELSE 'Mapped' END AS mapping_status,
       v.updatestatus AS last_change_status, v.updatedby AS last_updated_by, v.updateddate AS last_updated_date
FROM mdm.vw_vendor_reference_attributes v
WHERE v.isactive IS NOT FALSE;

CREATE OR REPLACE VIEW publish.item_master AS
SELECT i.source, i.item_id, i.item_no, i.item_description, i.item_english_name,
       i.cas_number, i.item_form,
       i.level_3, i.mdm_level_2 AS level_2, i.mdm_level_1 AS level_1, i.team, i.spend_area,
       i.addressable_spend, i.material_criticality_based_on_revenue, i.current_wave_plan,
       CASE WHEN COALESCE(i.level_3, '') = '' THEN 'Unmapped' ELSE 'Mapped' END AS mapping_status,
       i.updatestatus AS last_change_status, i.updatedby AS last_updated_by, i.updateddate AS last_updated_date
FROM mdm.vw_item_reference_attributes i
WHERE i.isactive IS NOT FALSE;

CREATE OR REPLACE VIEW publish.item_supplier_master AS
SELECT s.source, s.item_id, s.item_no, s.item_description,
       s.vendor_id, s.vendor_number, s.vendor_name,
       s.risk_transfer, s.supplier_specific_requirements, s.formula_non_formula,
       s.level_3, s.mdm_level_2 AS level_2, s.mdm_level_1 AS level_1, s.team, s.spend_area,
       s.updatestatus AS last_change_status, s.updatedby AS last_updated_by, s.updateddate AS last_updated_date
FROM mdm.vw_item_supplier_reference_attributes s
WHERE s.isactive IS NOT FALSE;

-- Mapped vs unmapped counts per object (BLITZDG step 9: Mapped / Unmapped Data)
CREATE OR REPLACE VIEW publish.mapping_summary AS
SELECT 'Vendor' AS object, mapping_status, COUNT(*) AS records FROM publish.vendor_master GROUP BY mapping_status
UNION ALL
SELECT 'Item', mapping_status, COUNT(*) FROM publish.item_master GROUP BY mapping_status;
