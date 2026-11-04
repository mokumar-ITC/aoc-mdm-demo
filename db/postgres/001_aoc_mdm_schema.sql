-- =====================================================================
-- AOC MDM - PostgreSQL schema migration (database: aoc_mdm)
-- Generated from AOC_MDM.Core/Entities/*.cs
-- Idempotent: safe to run more than once.
--
-- Object names mirror what the app builds: {catalog}.mdm.<table> and
-- {catalog}.intranet.databricks_fe_access_vw. With Catalog = "aoc_mdm" and
-- database = aoc_mdm, PostgreSQL resolves aoc_mdm.mdm.spend_area correctly.
-- Identifiers are unquoted (folded to lower case) so the app's unquoted SQL
-- (IsActive, UpdatedDate, ApprovalStatus, ...) matches.
-- =====================================================================

CREATE SCHEMA IF NOT EXISTS mdm;
CREATE SCHEMA IF NOT EXISTS intranet;

-- ---------------------------------------------------------------------
-- Taxonomy: Spend Area -> Team -> Level 1 -> Level 2 -> Level 3
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mdm.spend_area (
    spend_area          varchar(255) PRIMARY KEY,
    enabled             varchar(10),
    isactive            boolean      DEFAULT true,
    updatestatus        varchar(50),
    lastmodifiedbyrole  varchar(50),
    remarks             text,
    createdby           varchar(255),
    updatedby           varchar(255),
    createddate         timestamp    DEFAULT now(),
    updateddate         timestamp
);

CREATE TABLE IF NOT EXISTS mdm.team (
    team                varchar(255) PRIMARY KEY,
    spend_area          varchar(255),
    enabled             varchar(10),
    isactive            boolean      DEFAULT true,
    updatestatus        varchar(50),
    lastmodifiedbyrole  varchar(50),
    remarks             text,
    createdby           varchar(255),
    updatedby           varchar(255),
    createddate         timestamp    DEFAULT now(),
    updateddate         timestamp
);

CREATE TABLE IF NOT EXISTS mdm.aoc_level_1 (
    level_1             varchar(255) PRIMARY KEY,
    team                varchar(255),
    enabled             varchar(10),
    isactive            boolean      DEFAULT true,
    updatestatus        varchar(50),
    lastmodifiedbyrole  varchar(50),
    remarks             text,
    createdby           varchar(255),
    updatedby           varchar(255),
    createddate         timestamp    DEFAULT now(),
    updateddate         timestamp
);

CREATE TABLE IF NOT EXISTS mdm.aoc_level_2 (
    level_2             varchar(255) PRIMARY KEY,
    level_1             varchar(255) NOT NULL,
    enabled             varchar(10),
    isactive            boolean      DEFAULT true,
    updatestatus        varchar(50),
    lastmodifiedbyrole  varchar(50),
    remarks             text,
    createdby           varchar(255),
    updatedby           varchar(255),
    createddate         timestamp    DEFAULT now(),
    updateddate         timestamp
);

CREATE TABLE IF NOT EXISTS mdm.aoc_level_3 (
    level_3                                  varchar(255) PRIMARY KEY,
    level_2                                  varchar(255) NOT NULL,
    feedstock_1                              varchar(255),
    feedstock_2                              varchar(255),
    feedstock_3                              varchar(255),
    hazardous                                varchar(10),
    conflict_mineral_forced_labor_reg_check  varchar(10),
    enabled                                  varchar(10),
    isactive                                 boolean      DEFAULT true,
    updatestatus                             varchar(50),
    lastmodifiedbyrole                       varchar(50),
    remarks                                  text,
    createdby                                varchar(255),
    updatedby                                varchar(255),
    createddate                              timestamp    DEFAULT now(),
    updateddate                              timestamp
);

-- ---------------------------------------------------------------------
-- Vendor reference attributes (+ maker-checker audit)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mdm.vendor_reference_attributes (
    vendor_id                         varchar(100) NOT NULL,
    source                            varchar(100) NOT NULL DEFAULT 'sql_db',
    supplier_vendor                   varchar(255),
    vendor_english_name               varchar(500),
    vendor_family_name                varchar(500),
    level_3                           varchar(255),
    parent_company                    varchar(500),
    addressable_spend                 varchar(50),
    payables_very_important_supplier  varchar(50),
    intercompany                      varchar(50),
    indirect_spend_owner              varchar(255),
    isactive                          boolean      DEFAULT true,
    updatestatus                      varchar(50),
    lastmodifiedbyrole                varchar(50),
    remarks                           text,
    createdby                         varchar(255),
    updatedby                         varchar(255),
    createddate                       timestamp    DEFAULT now(),
    updateddate                       timestamp,
    PRIMARY KEY (vendor_id, source)
);

CREATE TABLE IF NOT EXISTS mdm.vendor_reference_attributes_audit (
    auditid                           bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    vendor_id                         varchar(100),
    source                            varchar(100),
    supplier_vendor                   varchar(255),
    vendor_english_name               varchar(500),
    vendor_family_name                varchar(500),
    level_3                           varchar(255),
    parent_company                    varchar(500),
    addressable_spend                 varchar(50),
    payables_very_important_supplier  varchar(50),
    intercompany                      varchar(50),
    indirect_spend_owner              varchar(255),
    isactive                          boolean      DEFAULT true,
    changetype                        varchar(20),
    approvalstatus                    varchar(50),
    makerid                           varchar(255),
    checkerid                         varchar(255),
    changetimestamp                   timestamp,
    approvaltimestamp                 timestamp,
    lastmodifiedbyrole                varchar(50),
    remarks                           text,
    createdby                         varchar(255),
    updatedby                         varchar(255),
    createddate                       timestamp,
    updateddate                       timestamp
);
CREATE INDEX IF NOT EXISTS ix_vendor_audit_status ON mdm.vendor_reference_attributes_audit (approvalstatus);
CREATE INDEX IF NOT EXISTS ix_vendor_audit_key    ON mdm.vendor_reference_attributes_audit (vendor_id, source);

-- ---------------------------------------------------------------------
-- Item reference attributes (+ audit)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mdm.item_reference_attributes (
    source                                 varchar(100) NOT NULL,
    item_id                                varchar(100) NOT NULL,
    item_english_name                      varchar(500),
    cas_number                             varchar(100),
    item_form                              varchar(100),
    level_3                                varchar(255),
    addressable_spend                      varchar(50),
    material_criticality_based_on_revenue  varchar(100),
    current_wave_plan                      varchar(100),
    isactive                               boolean      DEFAULT true,
    updatestatus                           varchar(50),
    lastmodifiedbyrole                     varchar(50),
    remarks                                text,
    createdby                              varchar(255),
    updatedby                              varchar(255),
    createddate                            timestamp    DEFAULT now(),
    updateddate                            timestamp,
    PRIMARY KEY (source, item_id)
);

CREATE TABLE IF NOT EXISTS mdm.item_reference_attributes_audit (
    auditid                                bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    source                                 varchar(100),
    item_id                                varchar(100),
    item_english_name                      varchar(500),
    cas_number                             varchar(100),
    item_form                              varchar(100),
    level_3                                varchar(255),
    addressable_spend                      varchar(50),
    material_criticality_based_on_revenue  varchar(100),
    current_wave_plan                      varchar(100),
    isactive                               boolean      DEFAULT true,
    changetype                             varchar(20),
    approvalstatus                         varchar(50),
    makerid                                varchar(255),
    checkerid                              varchar(255),
    changetimestamp                        timestamp,
    approvaltimestamp                      timestamp,
    lastmodifiedbyrole                     varchar(50),
    remarks                                text,
    createdby                              varchar(255),
    updatedby                              varchar(255),
    createddate                            timestamp,
    updateddate                            timestamp
);
CREATE INDEX IF NOT EXISTS ix_item_audit_status ON mdm.item_reference_attributes_audit (approvalstatus);
CREATE INDEX IF NOT EXISTS ix_item_audit_key    ON mdm.item_reference_attributes_audit (item_id, source);

-- ---------------------------------------------------------------------
-- Item-supplier reference attributes (+ audit)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mdm.item_supplier_reference_attributes (
    item_id                         varchar(100) NOT NULL,
    source                          varchar(100) NOT NULL,
    vendor_id                       varchar(100) NOT NULL,
    risk_transfer                   varchar(100),
    supplier_specific_requirements  text,
    formula_non_formula             varchar(50),
    isactive                        boolean      DEFAULT true,
    updatestatus                    varchar(50),
    lastmodifiedbyrole              varchar(50),
    remarks                         text,
    createdby                       varchar(255),
    updatedby                       varchar(255),
    createddate                     timestamp    DEFAULT now(),
    updateddate                     timestamp,
    PRIMARY KEY (item_id, source, vendor_id)
);

CREATE TABLE IF NOT EXISTS mdm.item_supplier_reference_attributes_audit (
    auditid                         bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    item_id                         varchar(100),
    source                          varchar(100),
    vendor_id                       varchar(100),
    risk_transfer                   varchar(100),
    supplier_specific_requirements  text,
    formula_non_formula             varchar(50),
    isactive                        boolean      DEFAULT true,
    changetype                      varchar(20),
    approvalstatus                  varchar(50),
    makerid                         varchar(255),
    checkerid                       varchar(255),
    changetimestamp                 timestamp,
    approvaltimestamp               timestamp,
    lastmodifiedbyrole              varchar(50),
    remarks                         text,
    createdby                       varchar(255),
    updatedby                       varchar(255),
    createddate                     timestamp,
    updateddate                     timestamp
);
CREATE INDEX IF NOT EXISTS ix_itemsup_audit_status ON mdm.item_supplier_reference_attributes_audit (approvalstatus);
CREATE INDEX IF NOT EXISTS ix_itemsup_audit_key    ON mdm.item_supplier_reference_attributes_audit (item_id, source, vendor_id);

-- ---------------------------------------------------------------------
-- ERP source stubs.
-- ASSUMPTION: the vw_* views expose vendor_name / vendor_number and
-- item_no / item_description, which are not in any entity table. In
-- Databricks they came from ERP data. These stubs stand in for that feed;
-- replace them with the real source (e.g. Oracle EBS) when it's available.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mdm.erp_vendor (
    vendor_id      varchar(100) NOT NULL,
    source         varchar(100) NOT NULL,
    vendor_name    varchar(500),
    vendor_number  varchar(100),
    PRIMARY KEY (vendor_id, source)
);

CREATE TABLE IF NOT EXISTS mdm.erp_item (
    item_id           varchar(100) NOT NULL,
    source            varchar(100) NOT NULL,
    item_no           varchar(100),
    item_description  varchar(1000),
    PRIMARY KEY (item_id, source)
);

-- ---------------------------------------------------------------------
-- Views used by the list / bulk-update screens
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW mdm.vw_aoc_level_1 AS
SELECT l1.level_1, l1.team, t.spend_area, l1.enabled, l1.isactive,
       l1.updatestatus, l1.lastmodifiedbyrole, l1.remarks,
       l1.createdby, l1.updatedby, l1.createddate, l1.updateddate
FROM mdm.aoc_level_1 l1
LEFT JOIN mdm.team t ON t.team = l1.team;

CREATE OR REPLACE VIEW mdm.vw_aoc_level_2 AS
SELECT l2.level_2, l2.level_1, l1.team, t.spend_area, l2.enabled, l2.isactive,
       l2.updatestatus, l2.lastmodifiedbyrole, l2.remarks,
       l2.createdby, l2.updatedby, l2.createddate, l2.updateddate
FROM mdm.aoc_level_2 l2
LEFT JOIN mdm.aoc_level_1 l1 ON l1.level_1 = l2.level_1
LEFT JOIN mdm.team t         ON t.team     = l1.team;

CREATE OR REPLACE VIEW mdm.vw_aoc_level_3 AS
SELECT l3.level_3, l3.level_2, l2.level_1, l1.team, t.spend_area,
       l3.feedstock_1, l3.feedstock_2, l3.feedstock_3, l3.hazardous,
       l3.conflict_mineral_forced_labor_reg_check, l3.enabled, l3.isactive,
       l3.updatestatus, l3.lastmodifiedbyrole, l3.remarks,
       l3.createdby, l3.updatedby, l3.createddate, l3.updateddate
FROM mdm.aoc_level_3 l3
LEFT JOIN mdm.aoc_level_2 l2 ON l2.level_2 = l3.level_2
LEFT JOIN mdm.aoc_level_1 l1 ON l1.level_1 = l2.level_1
LEFT JOIN mdm.team t         ON t.team     = l1.team;

CREATE OR REPLACE VIEW mdm.vw_vendor_reference_attributes AS
SELECT v.vendor_id, v.source, v.supplier_vendor, v.vendor_english_name,
       v.vendor_family_name, v.level_3, v.parent_company, v.addressable_spend,
       v.payables_very_important_supplier, v.intercompany, v.indirect_spend_owner,
       e.vendor_name, e.vendor_number,
       h.level_2 AS mdm_level_2, h.level_1 AS mdm_level_1, h.team, h.spend_area,
       v.isactive, v.updatestatus, v.lastmodifiedbyrole, v.remarks,
       v.createdby, v.updatedby, v.createddate, v.updateddate
FROM mdm.vendor_reference_attributes v
LEFT JOIN mdm.erp_vendor e     ON e.vendor_id = v.vendor_id AND e.source = v.source
LEFT JOIN mdm.vw_aoc_level_3 h ON h.level_3   = v.level_3;

CREATE OR REPLACE VIEW mdm.vw_item_reference_attributes AS
SELECT i.source, i.item_id, i.item_english_name, i.cas_number, i.item_form,
       i.level_3, i.addressable_spend, i.material_criticality_based_on_revenue,
       i.current_wave_plan, e.item_no, e.item_description,
       h.level_2 AS mdm_level_2, h.level_1 AS mdm_level_1, h.team, h.spend_area,
       i.isactive, i.updatestatus, i.lastmodifiedbyrole, i.remarks,
       i.createdby, i.updatedby, i.createddate, i.updateddate
FROM mdm.item_reference_attributes i
LEFT JOIN mdm.erp_item e       ON e.item_id = i.item_id AND e.source = i.source
LEFT JOIN mdm.vw_aoc_level_3 h ON h.level_3 = i.level_3;

CREATE OR REPLACE VIEW mdm.vw_item_supplier_reference_attributes AS
SELECT s.source, s.item_id, s.vendor_id, s.risk_transfer,
       s.supplier_specific_requirements, s.formula_non_formula,
       ev.vendor_name, ev.vendor_number, ei.item_no, ei.item_description,
       i.level_3, h.level_2 AS mdm_level_2, h.level_1 AS mdm_level_1, h.team, h.spend_area,
       s.isactive, s.updatestatus, s.lastmodifiedbyrole, s.remarks,
       s.createdby, s.updatedby, s.createddate, s.updateddate
FROM mdm.item_supplier_reference_attributes s
LEFT JOIN mdm.erp_vendor ev                ON ev.vendor_id = s.vendor_id AND ev.source = s.source
LEFT JOIN mdm.erp_item ei                  ON ei.item_id   = s.item_id   AND ei.source = s.source
LEFT JOIN mdm.item_reference_attributes i  ON i.item_id    = s.item_id   AND i.source  = s.source
LEFT JOIN mdm.vw_aoc_level_3 h             ON h.level_3    = i.level_3;

-- ---------------------------------------------------------------------
-- User roles (read by RoleRequirementFilter when EnableRoleCheck = true)
-- Levels: 1 = Read, 2 = Maker, 3 = Approver, 4 = Admin
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS intranet.user_access (
    login      varchar(255) PRIMARY KEY,   -- Windows login without DOMAIN\
    levelno    integer      NOT NULL CHECK (levelno BETWEEN 1 AND 4),
    leveldesc  varchar(50)  NOT NULL
);

CREATE OR REPLACE VIEW intranet.databricks_fe_access_vw AS
SELECT login, levelno, leveldesc FROM intranet.user_access;

-- Example (not executed):
-- INSERT INTO intranet.user_access VALUES ('abishnoi', 4, 'Admin');
