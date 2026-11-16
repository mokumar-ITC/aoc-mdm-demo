"""Generate db/postgres/008_product_master_seed.sql from 'Product_Master v2.0.xlsx'.

Usage (from the repo root):
    pip install openpyxl
    python db/postgres/tools/generate_product_master_seed.py ["Product_Master v2.0.xlsx"]

Cleaning rules applied to the hierarchy columns (Product_Family, Product_Type, Model, Size):
  * leading/trailing spaces and non-breaking spaces are trimmed;
  * a blank level at the end of the path is dropped, so the product hangs off the
    deepest level it has (e.g. Commissioning records have no Size);
  * a blank level in the middle of the path (RDHx has a Size but no Model) becomes
    "(Not specified)" so the deeper level still has a parent.
"""
import sys
from pathlib import Path

import openpyxl

REPO = Path(__file__).resolve().parents[3]
SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / "Product_Master v2.0.xlsx"
TARGET = REPO / "db" / "postgres" / "008_product_master_seed.sql"
NOT_SPECIFIED = "(Not specified)"

HEADERS = ["Product_Family", "Product_Type", "Model", "Size", "SIOP_Model", "SIOP_Tier", "Product_Name",
           "Product_Code", "Sections_Per_Unit", "Sale_Price_Per_Unit", "Sale_Price_Per_Hour",
           "Default_Supply_Source", "Est_Fab_Hours", "Est_Assembly_Hours", "Fab_Lbr_Hrs_%", "Assmb_Lbr_Hrs_%",
           "Qty_Fans", "Qty_Coils", "Qty_Panels", "Standard_Unit_Multiplier", "Status", "Capacity Group"]

# workbook header -> column in mdm.product_master (hierarchy fields map to their level)
COLUMN_FOR = {
    "Product_Family": "product_hierarchy (level 1)", "Product_Type": "product_hierarchy (level 2)",
    "Model": "product_hierarchy (level 3)", "Size": "product_hierarchy (level 4)",
    "Fab_Lbr_Hrs_%": "fab_lbr_hrs_pct", "Assmb_Lbr_Hrs_%": "assmb_lbr_hrs_pct", "Capacity Group": "capacity_group",
}


def clean(value):
    if value is None:
        return None
    text = str(value).replace("\xa0", " ").strip()
    return text or None


def sql(value):
    if value is None:
        return "NULL"
    if isinstance(value, (int, float)):
        return str(value)
    return "'" + str(value).replace("'", "''") + "'"


def number(value):
    value = clean(value)
    if value is None:
        return None
    n = float(value)
    return int(n) if n.is_integer() else n


def hierarchy(row):
    levels = [clean(row[i]) for i in range(4)]
    while levels and levels[-1] is None:
        levels.pop()
    return [lvl if lvl is not None else NOT_SPECIFIED for lvl in levels]


def main():
    wb = openpyxl.load_workbook(SOURCE, data_only=True)
    ws = wb["Product Master"]
    header = [clean(c.value) for c in ws[1]]
    if header != HEADERS:
        sys.exit(f"Unexpected headers in 'Product Master': {header}")

    stage = []
    for row_no, row in enumerate(ws.iter_rows(min_row=2, values_only=True), start=2):
        if not any(clean(v) for v in row):
            continue
        levels = hierarchy(row) + [None] * 4
        stage.append([row_no, *levels[:4],
                      clean(row[6]), clean(row[7]), clean(row[4]), clean(row[5]),
                      *(number(row[i]) for i in range(8, 11)), clean(row[11]),
                      *(number(row[i]) for i in range(12, 20)), clean(row[20]), clean(row[21])])

    fields = []
    fws = wb["Product Master Fields"]
    in_table = False
    for name, description in fws.iter_rows(min_col=1, max_col=2, values_only=True):
        name = clean(name)
        if name == "Header Field":
            in_table = True
            continue
        if in_table and name in HEADERS:
            fields.append((HEADERS.index(name) + 1, name,
                           COLUMN_FOR.get(name, name.lower()), clean(description)))

    out = [
        "-- =====================================================================",
        f"-- Gradian Product Master - data, generated from '{SOURCE.name}'",
        "-- by db/postgres/tools/generate_product_master_seed.py. Do not edit by hand.",
        f"-- {len(stage)} products. Re-runnable: replaces all product master data.",
        "-- Run after 007_product_master_schema.sql.",
        "-- =====================================================================",
        "SET client_encoding = 'UTF8';",
        "BEGIN;",
        "",
        "TRUNCATE mdm.product_master_audit, mdm.product_master, mdm.product_hierarchy, mdm.product_master_field RESTART IDENTITY;",
        "",
        "CREATE TEMP TABLE product_stage (",
        "    source_row int, lvl1 text, lvl2 text, lvl3 text, lvl4 text,",
        "    product_name text, product_code text, siop_model text, siop_tier text,",
        "    sections_per_unit int, sale_price_per_unit numeric, sale_price_per_hour numeric,",
        "    default_supply_source text, est_fab_hours numeric, est_assembly_hours numeric,",
        "    fab_lbr_hrs_pct numeric, assmb_lbr_hrs_pct numeric, qty_fans int, qty_coils int, qty_panels int,",
        "    standard_unit_multiplier numeric, status text, capacity_group text",
        ") ON COMMIT DROP;",
        "",
        "INSERT INTO product_stage VALUES",
        ",\n".join("    (" + ", ".join(sql(v) for v in r) + ")" for r in stage) + ";",
        "",
        "-- Hierarchy, one level at a time; node_path is 'Family > Type > Model > Size'",
        "INSERT INTO mdm.product_hierarchy (parent_node_id, level_no, node_name, node_path, createdby)",
        "SELECT DISTINCT NULL::bigint, 1, lvl1, lvl1, 'product_master_seed' FROM product_stage;",
    ]
    for level in (2, 3, 4):
        parent_path = " || ' > ' || ".join(f"s.lvl{i}" for i in range(1, level))
        out += [
            "INSERT INTO mdm.product_hierarchy (parent_node_id, level_no, node_name, node_path, createdby)",
            f"SELECT DISTINCT p.node_id, {level}, s.lvl{level}, p.node_path || ' > ' || s.lvl{level}, 'product_master_seed'",
            f"  FROM product_stage s JOIN mdm.product_hierarchy p ON p.level_no = {level - 1} AND p.node_path = {parent_path}",
            f" WHERE s.lvl{level} IS NOT NULL;",
        ]
    out += [
        "",
        "INSERT INTO mdm.product_master (node_id, product_name, product_code, siop_model, siop_tier, sections_per_unit,",
        "       sale_price_per_unit, sale_price_per_hour, default_supply_source, est_fab_hours, est_assembly_hours,",
        "       fab_lbr_hrs_pct, assmb_lbr_hrs_pct, qty_fans, qty_coils, qty_panels, standard_unit_multiplier,",
        "       status, capacity_group, source_row, createdby)",
        "SELECT h.node_id, s.product_name, s.product_code, s.siop_model, s.siop_tier, s.sections_per_unit,",
        "       s.sale_price_per_unit, s.sale_price_per_hour, s.default_supply_source, s.est_fab_hours, s.est_assembly_hours,",
        "       s.fab_lbr_hrs_pct, s.assmb_lbr_hrs_pct, s.qty_fans, s.qty_coils, s.qty_panels, s.standard_unit_multiplier,",
        "       s.status, s.capacity_group, s.source_row, 'product_master_seed'",
        "  FROM product_stage s",
        "  JOIN mdm.product_hierarchy h ON h.node_path = concat_ws(' > ', s.lvl1, s.lvl2, s.lvl3, s.lvl4)",
        " ORDER BY s.source_row;",
        "",
        "-- Fail the load if any staged product did not find its hierarchy node",
        "DO $$",
        "BEGIN",
        "  IF (SELECT count(*) FROM mdm.product_master) <> (SELECT count(*) FROM product_stage) THEN",
        "    RAISE EXCEPTION 'Product master load incomplete: % of % products loaded',",
        "      (SELECT count(*) FROM mdm.product_master), (SELECT count(*) FROM product_stage);",
        "  END IF;",
        "END $$;",
        "",
        "INSERT INTO mdm.product_master_field (field_order, source_field, column_name, description) VALUES",
        ",\n".join("    (" + ", ".join(sql(v) for v in f) + ")" for f in fields) + ";",
        "",
        "COMMIT;",
        "",
    ]
    TARGET.write_text("\n".join(out), encoding="utf-8", newline="\n")
    print(f"Wrote {TARGET.relative_to(REPO)}: {len(stage)} products, {len(fields)} field descriptions")


if __name__ == "__main__":
    main()
