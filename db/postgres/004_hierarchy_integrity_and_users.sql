-- =====================================================================
-- AOC MDM - hierarchy integrity, cascades, history, and login accounts
-- Spend Area -> Team -> Level 1 -> Level 2 -> Level 3 -> (vendors, items)
-- Idempotent. Run after 001-003.
-- Any statement can attribute its changes to an app user with:
--   SELECT set_config('mdm.user', '<login>', true);   (inside a transaction)
-- =====================================================================

-- ---------------------------------------------------------------------
-- 1. Duplicate names: unique per level, case-insensitive ("Resins" = "RESINS")
-- ---------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS ux_spend_area_name ON mdm.spend_area  (lower(spend_area));
CREATE UNIQUE INDEX IF NOT EXISTS ux_team_name       ON mdm.team        (lower(team));
CREATE UNIQUE INDEX IF NOT EXISTS ux_level_1_name    ON mdm.aoc_level_1 (lower(level_1));
CREATE UNIQUE INDEX IF NOT EXISTS ux_level_2_name    ON mdm.aoc_level_2 (lower(level_2));
CREATE UNIQUE INDEX IF NOT EXISTS ux_level_3_name    ON mdm.aoc_level_3 (lower(level_3));

-- ---------------------------------------------------------------------
-- 2. Integrity + rename cascade: foreign keys
--    ON UPDATE CASCADE  -> renaming a level renames it everywhere below
--    ON DELETE RESTRICT -> a level cannot be deleted while anything uses it
-- ---------------------------------------------------------------------
DO $$
DECLARE fk record;
BEGIN
  FOR fk IN SELECT * FROM (VALUES
      ('fk_team_spend_area',  'mdm.team',                        'spend_area', 'mdm.spend_area',  'spend_area'),
      ('fk_level_1_team',     'mdm.aoc_level_1',                 'team',       'mdm.team',        'team'),
      ('fk_level_2_level_1',  'mdm.aoc_level_2',                 'level_1',    'mdm.aoc_level_1', 'level_1'),
      ('fk_level_3_level_2',  'mdm.aoc_level_3',                 'level_2',    'mdm.aoc_level_2', 'level_2'),
      ('fk_vendor_level_3',   'mdm.vendor_reference_attributes', 'level_3',    'mdm.aoc_level_3', 'level_3'),
      ('fk_item_level_3',     'mdm.item_reference_attributes',   'level_3',    'mdm.aoc_level_3', 'level_3'))
    AS t(name, child, child_col, parent, parent_col)
  LOOP
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = fk.name) THEN
      EXECUTE format('ALTER TABLE %s ADD CONSTRAINT %I FOREIGN KEY (%I) REFERENCES %s (%I) ON UPDATE CASCADE ON DELETE RESTRICT',
                     fk.child, fk.name, fk.child_col, fk.parent, fk.parent_col);
    END IF;
  END LOOP;
END $$;

-- ---------------------------------------------------------------------
-- 3. Enable / disable rules
--    - Disabling a level disables everything below it (trigger chain).
--    - A level cannot be enabled (or moved enabled) under a disabled parent.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION mdm.trg_cascade_disable() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  child_table text := TG_ARGV[0];
  child_col   text := TG_ARGV[1];
  key_col     text := TG_ARGV[2];
  key_val     text := to_jsonb(NEW) ->> key_col;
BEGIN
  IF NEW.enabled = 'No' AND OLD.enabled IS DISTINCT FROM 'No' THEN
    EXECUTE format('UPDATE %s SET enabled = ''No'', updatedby = $1, updateddate = now() WHERE %I = $2 AND enabled IS DISTINCT FROM ''No''',
                   child_table, child_col)
    USING split_part(coalesce(nullif(current_setting('mdm.user', true), ''), NEW.updatedby, 'system'), ' (cascade', 1)
          || ' (cascade from ' || key_val || ')', key_val;
  END IF;
  RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION mdm.trg_block_enable_under_disabled_parent() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  parent_table text := TG_ARGV[0];
  parent_col   text := TG_ARGV[1];
  fk_col       text := TG_ARGV[2];
  parent_val   text := to_jsonb(NEW) ->> fk_col;
  parent_enabled text;
BEGIN
  IF NEW.enabled = 'Yes' AND parent_val IS NOT NULL
     AND (TG_OP = 'INSERT' OR OLD.enabled IS DISTINCT FROM 'Yes' OR (to_jsonb(OLD) ->> fk_col) IS DISTINCT FROM parent_val) THEN
    EXECUTE format('SELECT enabled FROM %s WHERE %I = $1', parent_table, parent_col) INTO parent_enabled USING parent_val;
    IF parent_enabled = 'No' THEN
      RAISE EXCEPTION 'Cannot enable "%" because its parent "%" is disabled. Enable the parent first.',
        to_jsonb(NEW) ->> TG_ARGV[3], parent_val USING ERRCODE = 'P0001';
    END IF;
  END IF;
  RETURN NEW;
END $$;

DROP TRIGGER IF EXISTS cascade_disable ON mdm.spend_area;
CREATE TRIGGER cascade_disable AFTER UPDATE OF enabled ON mdm.spend_area
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_cascade_disable('mdm.team', 'spend_area', 'spend_area');
DROP TRIGGER IF EXISTS cascade_disable ON mdm.team;
CREATE TRIGGER cascade_disable AFTER UPDATE OF enabled ON mdm.team
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_cascade_disable('mdm.aoc_level_1', 'team', 'team');
DROP TRIGGER IF EXISTS cascade_disable ON mdm.aoc_level_1;
CREATE TRIGGER cascade_disable AFTER UPDATE OF enabled ON mdm.aoc_level_1
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_cascade_disable('mdm.aoc_level_2', 'level_1', 'level_1');
DROP TRIGGER IF EXISTS cascade_disable ON mdm.aoc_level_2;
CREATE TRIGGER cascade_disable AFTER UPDATE OF enabled ON mdm.aoc_level_2
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_cascade_disable('mdm.aoc_level_3', 'level_2', 'level_2');

DROP TRIGGER IF EXISTS block_enable ON mdm.team;
CREATE TRIGGER block_enable BEFORE INSERT OR UPDATE ON mdm.team
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_block_enable_under_disabled_parent('mdm.spend_area', 'spend_area', 'spend_area', 'team');
DROP TRIGGER IF EXISTS block_enable ON mdm.aoc_level_1;
CREATE TRIGGER block_enable BEFORE INSERT OR UPDATE ON mdm.aoc_level_1
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_block_enable_under_disabled_parent('mdm.team', 'team', 'team', 'level_1');
DROP TRIGGER IF EXISTS block_enable ON mdm.aoc_level_2;
CREATE TRIGGER block_enable BEFORE INSERT OR UPDATE ON mdm.aoc_level_2
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_block_enable_under_disabled_parent('mdm.aoc_level_1', 'level_1', 'level_1', 'level_2');
DROP TRIGGER IF EXISTS block_enable ON mdm.aoc_level_3;
CREATE TRIGGER block_enable BEFORE INSERT OR UPDATE ON mdm.aoc_level_3
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_block_enable_under_disabled_parent('mdm.aoc_level_2', 'level_2', 'level_2', 'level_3');

-- ---------------------------------------------------------------------
-- 4. Hierarchy history: every insert / update / rename / delete
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mdm.hierarchy_audit (
    audit_id    bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    level_name  varchar(30)  NOT NULL,      -- Spend Area, Team, Level 1, Level 2, Level 3
    record_key  varchar(255) NOT NULL,      -- name after the change (before, for deletes)
    action      varchar(20)  NOT NULL,      -- INSERT, UPDATE, RENAME, DELETE
    changes     text,                       -- "enabled: Yes -> No; level_1: A -> B"
    old_values  jsonb,
    new_values  jsonb,
    changed_by  varchar(255),
    changed_at  timestamp    NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_hierarchy_audit_at ON mdm.hierarchy_audit (changed_at DESC);

CREATE OR REPLACE FUNCTION mdm.trg_hierarchy_audit() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  level_name text := TG_ARGV[0];
  key_col    text := TG_ARGV[1];
  o jsonb := CASE WHEN TG_OP <> 'INSERT' THEN to_jsonb(OLD) - 'createddate' - 'updateddate' END;
  n jsonb := CASE WHEN TG_OP <> 'DELETE' THEN to_jsonb(NEW) - 'createddate' - 'updateddate' END;
  app_user text := nullif(current_setting('mdm.user', true), '');
  act text := TG_OP;
  diff text;
BEGIN
  IF TG_OP = 'UPDATE' THEN
    SELECT string_agg(k || ': ' || coalesce(o ->> k, '(empty)') || ' -> ' || coalesce(n ->> k, '(empty)'), '; ' ORDER BY k)
      INTO diff
      FROM jsonb_object_keys(n) k
     WHERE k NOT IN ('updatedby') AND (o -> k) IS DISTINCT FROM (n -> k);
    IF diff IS NULL THEN RETURN NEW; END IF;          -- nothing business-relevant changed
    IF (o ->> key_col) IS DISTINCT FROM (n ->> key_col) THEN act := 'RENAME'; END IF;
  END IF;

  INSERT INTO mdm.hierarchy_audit (level_name, record_key, action, changes, old_values, new_values, changed_by)
  VALUES (level_name,
          coalesce(n ->> key_col, o ->> key_col),
          act,
          diff,
          o, n,
          CASE TG_OP
            WHEN 'INSERT' THEN coalesce(app_user, NEW.createdby, 'system')
            WHEN 'DELETE' THEN coalesce(app_user, 'system')
            ELSE coalesce(app_user, NEW.updatedby, 'system')
          END);
  RETURN NULL;   -- AFTER trigger: return value is ignored
END $$;

DROP TRIGGER IF EXISTS hierarchy_audit ON mdm.spend_area;
CREATE TRIGGER hierarchy_audit AFTER INSERT OR UPDATE OR DELETE ON mdm.spend_area
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_hierarchy_audit('Spend Area', 'spend_area');
DROP TRIGGER IF EXISTS hierarchy_audit ON mdm.team;
CREATE TRIGGER hierarchy_audit AFTER INSERT OR UPDATE OR DELETE ON mdm.team
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_hierarchy_audit('Team', 'team');
DROP TRIGGER IF EXISTS hierarchy_audit ON mdm.aoc_level_1;
CREATE TRIGGER hierarchy_audit AFTER INSERT OR UPDATE OR DELETE ON mdm.aoc_level_1
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_hierarchy_audit('Level 1', 'level_1');
DROP TRIGGER IF EXISTS hierarchy_audit ON mdm.aoc_level_2;
CREATE TRIGGER hierarchy_audit AFTER INSERT OR UPDATE OR DELETE ON mdm.aoc_level_2
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_hierarchy_audit('Level 2', 'level_2');
DROP TRIGGER IF EXISTS hierarchy_audit ON mdm.aoc_level_3;
CREATE TRIGGER hierarchy_audit AFTER INSERT OR UPDATE OR DELETE ON mdm.aoc_level_3
  FOR EACH ROW EXECUTE FUNCTION mdm.trg_hierarchy_audit('Level 3', 'level_3');

-- ---------------------------------------------------------------------
-- 5. Login accounts (forms login). The role check keeps reading
--    intranet.databricks_fe_access_vw, which now only shows active users.
--    Levels: 1 = Viewer, 2 = Maker, 3 = Checker, 4 = Admin
-- ---------------------------------------------------------------------
ALTER TABLE intranet.user_access ADD COLUMN IF NOT EXISTS display_name  varchar(255);
ALTER TABLE intranet.user_access ADD COLUMN IF NOT EXISTS password_hash varchar(255);
ALTER TABLE intranet.user_access ADD COLUMN IF NOT EXISTS is_active     boolean NOT NULL DEFAULT true;
ALTER TABLE intranet.user_access ADD COLUMN IF NOT EXISTS last_login    timestamp;
ALTER TABLE intranet.user_access ADD COLUMN IF NOT EXISTS created_date  timestamp NOT NULL DEFAULT now();
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_access_login ON intranet.user_access (lower(login));

CREATE OR REPLACE VIEW intranet.databricks_fe_access_vw AS
SELECT login, levelno, leveldesc FROM intranet.user_access WHERE is_active;
