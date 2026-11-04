-- =====================================================================
-- AOC MDM - test data for local testing (database: aoc_mdm)
-- Idempotent: rows that already exist are left as they are.
-- =====================================================================

-- Taxonomy: Spend Area -> Team -> Level 1 -> Level 2 -> Level 3
INSERT INTO mdm.spend_area (spend_area, enabled, isactive, createdby, createddate, updateddate) VALUES
 ('Direct Materials',   'Yes', true, 'seed', now(), now()),
 ('Indirect Materials', 'Yes', true, 'seed', now(), now()),
 ('Services',           'Yes', true, 'seed', now(), now()),
 ('Logistics',          'No',  true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO mdm.team (team, spend_area, enabled, isactive, createdby, createddate, updateddate) VALUES
 ('Chemicals Team',     'Direct Materials',   'Yes', true, 'seed', now(), now()),
 ('Packaging Team',     'Direct Materials',   'Yes', true, 'seed', now(), now()),
 ('MRO Team',           'Indirect Materials', 'Yes', true, 'seed', now(), now()),
 ('Professional Svcs',  'Services',           'Yes', true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO mdm.aoc_level_1 (level_1, team, enabled, isactive, createdby, createddate, updateddate) VALUES
 ('Resins',             'Chemicals Team',    'Yes', true, 'seed', now(), now()),
 ('Additives',          'Chemicals Team',    'Yes', true, 'seed', now(), now()),
 ('Containers',         'Packaging Team',    'Yes', true, 'seed', now(), now()),
 ('Maintenance',        'MRO Team',          'Yes', true, 'seed', now(), now()),
 ('Consulting',         'Professional Svcs', 'Yes', true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO mdm.aoc_level_2 (level_2, level_1, enabled, isactive, createdby, createddate, updateddate) VALUES
 ('Unsaturated Polyester', 'Resins',      'Yes', true, 'seed', now(), now()),
 ('Vinyl Ester',           'Resins',      'Yes', true, 'seed', now(), now()),
 ('Catalysts',             'Additives',   'Yes', true, 'seed', now(), now()),
 ('Drums',                 'Containers',  'Yes', true, 'seed', now(), now()),
 ('Spare Parts',           'Maintenance', 'Yes', true, 'seed', now(), now()),
 ('IT Consulting',         'Consulting',  'Yes', true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO mdm.aoc_level_3 (level_3, level_2, feedstock_1, feedstock_2, feedstock_3, hazardous,
                             conflict_mineral_forced_labor_reg_check, enabled, isactive, createdby, createddate, updateddate) VALUES
 ('Orthophthalic UPR',  'Unsaturated Polyester', 'Phthalic Anhydride', 'Glycol', 'Styrene', 'Yes', 'No',  'Yes', true, 'seed', now(), now()),
 ('Isophthalic UPR',    'Unsaturated Polyester', 'Isophthalic Acid',   'Glycol', 'Styrene', 'Yes', 'No',  'Yes', true, 'seed', now(), now()),
 ('Epoxy Vinyl Ester',  'Vinyl Ester',           'Epoxy',              'Methacrylic Acid', NULL, 'Yes', 'No', 'Yes', true, 'seed', now(), now()),
 ('MEKP Catalyst',      'Catalysts',             'MEK',                'Hydrogen Peroxide', NULL, 'Yes', 'Yes', 'Yes', true, 'seed', now(), now()),
 ('Steel Drums',        'Drums',                 'Steel',              NULL, NULL, 'No',  'Yes', 'Yes', true, 'seed', now(), now()),
 ('Pump Parts',         'Spare Parts',           NULL,                 NULL, NULL, 'No',  'No',  'Yes', true, 'seed', now(), now()),
 ('ERP Consulting',     'IT Consulting',         NULL,                 NULL, NULL, 'No',  'No',  'Yes', true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

-- ERP source stubs (replace with a feed from Fabric lh_acc_dev_gold later)
INSERT INTO mdm.erp_vendor (vendor_id, source, vendor_name, vendor_number) VALUES
 ('V1001', 'sql_db', 'ACME CHEMICAL CORP',       '1001'),
 ('V1002', 'sql_db', 'GLOBAL RESINS LTD',        '1002'),
 ('V1003', 'sql_db', 'PACKRIGHT DRUMS INC',      '1003'),
 ('V1004', 'sql_db', 'PUMPWORKS INDUSTRIAL',     '1004'),
 ('V1005', 'sql_db', 'NORTHWIND CONSULTING LLC', '1005')
ON CONFLICT DO NOTHING;

INSERT INTO mdm.erp_item (item_id, source, item_no, item_description) VALUES
 ('I2001', 'sql_db', 'RM-2001', 'Phthalic anhydride flakes 25kg'),
 ('I2002', 'sql_db', 'RM-2002', 'Isophthalic acid powder'),
 ('I2003', 'sql_db', 'RM-2003', 'MEKP catalyst 9% active oxygen'),
 ('I2004', 'sql_db', 'PK-2004', '55 gal open-head steel drum'),
 ('I2005', 'sql_db', 'SP-2005', 'Centrifugal pump seal kit')
ON CONFLICT DO NOTHING;

-- Vendor / Item / Item-supplier attributes
INSERT INTO mdm.vendor_reference_attributes (vendor_id, source, supplier_vendor, vendor_english_name, vendor_family_name,
       level_3, parent_company, addressable_spend, payables_very_important_supplier, intercompany, indirect_spend_owner,
       isactive, createdby, createddate, updateddate) VALUES
 ('V1001', 'sql_db', 'Supplier', 'Acme Chemical',      'Acme',      'Orthophthalic UPR', 'Acme Holdings',     'Yes', 'Yes', 'No', NULL,          true, 'seed', now(), now()),
 ('V1002', 'sql_db', 'Supplier', 'Global Resins',      'Global',    'Isophthalic UPR',   'Global Group',      'Yes', 'No',  'No', NULL,          true, 'seed', now(), now()),
 ('V1003', 'sql_db', 'Supplier', 'PackRight Drums',    'PackRight', 'Steel Drums',       'PackRight Inc',     'Yes', 'No',  'No', NULL,          true, 'seed', now(), now()),
 ('V1004', 'sql_db', 'Supplier', 'PumpWorks',          'PumpWorks', 'Pump Parts',        'PumpWorks Group',   'No',  'No',  'No', 'MRO Team',    true, 'seed', now(), now()),
 ('V1005', 'sql_db', 'Vendor',   'Northwind Consulting','Northwind','ERP Consulting',    'Northwind Partners','Yes', 'No',  'No', 'IT Director', true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO mdm.item_reference_attributes (source, item_id, item_english_name, cas_number, item_form, level_3,
       addressable_spend, material_criticality_based_on_revenue, current_wave_plan, isactive, createdby, createddate, updateddate) VALUES
 ('sql_db', 'I2001', 'Phthalic Anhydride', '85-44-9',   'Solid',  'Orthophthalic UPR', 'Yes', 'High',   'Wave 1', true, 'seed', now(), now()),
 ('sql_db', 'I2002', 'Isophthalic Acid',   '121-91-5',  'Powder', 'Isophthalic UPR',   'Yes', 'High',   'Wave 1', true, 'seed', now(), now()),
 ('sql_db', 'I2003', 'MEKP',               '1338-23-4', 'Liquid', 'MEKP Catalyst',     'Yes', 'Medium', 'Wave 2', true, 'seed', now(), now()),
 ('sql_db', 'I2004', 'Steel Drum 55 gal',  NULL,        'Solid',  'Steel Drums',       'Yes', 'Low',    'Wave 3', true, 'seed', now(), now()),
 ('sql_db', 'I2005', 'Pump Seal Kit',      NULL,        'Solid',  'Pump Parts',        'No',  'Low',    NULL,     true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO mdm.item_supplier_reference_attributes (item_id, source, vendor_id, risk_transfer, supplier_specific_requirements,
       formula_non_formula, isactive, createdby, createddate, updateddate) VALUES
 ('I2001', 'sql_db', 'V1001', 'FOB Origin',      'COA required per lot', 'Formula',     true, 'seed', now(), now()),
 ('I2002', 'sql_db', 'V1002', 'FOB Destination', NULL,                   'Formula',     true, 'seed', now(), now()),
 ('I2003', 'sql_db', 'V1001', 'FOB Origin',      'Hazmat shipping',      'Formula',     true, 'seed', now(), now()),
 ('I2004', 'sql_db', 'V1003', 'FOB Destination', NULL,                   'Non-Formula', true, 'seed', now(), now()),
 ('I2005', 'sql_db', 'V1004', 'FOB Destination', NULL,                   'Non-Formula', true, 'seed', now(), now())
ON CONFLICT DO NOTHING;

-- Unmapped records (no Level 3 yet) for the "Unmapped Vendors / Items" screens
INSERT INTO mdm.erp_vendor (vendor_id, source, vendor_name, vendor_number) VALUES
 ('V1006', 'sql_db', 'NEWCO SUPPLIES INC', '1006')
ON CONFLICT DO NOTHING;
INSERT INTO mdm.vendor_reference_attributes (vendor_id, source, supplier_vendor, vendor_english_name, vendor_family_name,
       level_3, parent_company, isactive, createdby, createddate, updateddate) VALUES
 ('V1006', 'sql_db', 'Supplier', 'NewCo Supplies', 'NewCo', NULL, NULL, true, 'seed', now(), now())
ON CONFLICT DO NOTHING;
INSERT INTO mdm.erp_item (item_id, source, item_no, item_description) VALUES
 ('I2006', 'sql_db', 'RM-2006', 'Unclassified solvent blend')
ON CONFLICT DO NOTHING;
INSERT INTO mdm.item_reference_attributes (source, item_id, item_english_name, level_3, isactive, createdby, createddate, updateddate) VALUES
 ('sql_db', 'I2006', 'Solvent Blend', NULL, true, 'seed', now(), now())
ON CONFLICT DO NOTHING;
