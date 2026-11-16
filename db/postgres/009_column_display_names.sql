-- =====================================================================
-- AOC MDM - admin-managed column display names (Administration > Column Editing)
-- display_name is the label shown on screens; NULL means the built-in label.
-- Only the label changes: database column names and publish views stay as they are.
-- Run after 006_column_edit_settings.sql. Safe to run more than once.
-- =====================================================================
ALTER TABLE mdm.column_edit_setting ADD COLUMN IF NOT EXISTS display_name varchar(100);
