-- =====================================================================
-- AOC MDM - admin-managed column locks (Administration > Column Editing)
-- A column with no row here is editable. Safe to run more than once.
-- =====================================================================
CREATE TABLE IF NOT EXISTS mdm.column_edit_setting (
    table_key    varchar(50)  NOT NULL,
    column_key   varchar(100) NOT NULL,
    is_editable  boolean      NOT NULL DEFAULT true,
    updated_by   varchar(255),
    updated_date timestamp    NOT NULL DEFAULT now(),
    PRIMARY KEY (table_key, column_key)
);
