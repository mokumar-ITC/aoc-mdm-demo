# AOC MDM

A master data management (MDM) web app for maintaining vendor, item and item-supplier reference data, along with the spend hierarchy they belong to. Changes go through a maker/checker approval step, and approved data is published to downstream systems through read-only views.

The hierarchy is: Spend Area → Team → Level 1 → Level 2 → Level 3 → vendors and items.

## Gradian Product Master (demo)

A read-only **Product Master** module shows Gradian's product hierarchy, loaded from `Product_Master v2.0.xlsx`:

Product Family → Product Type → Model → Size → Product

It has its own tables and doesn't use the spend hierarchy or the vendor and item masters:

| Object | Holds |
|---|---|
| `mdm.product_hierarchy_level` | The four level names |
| `mdm.product_hierarchy` | One row per hierarchy node, linked to its parent |
| `mdm.product_master` | One row per product, linked to its deepest hierarchy node |
| `mdm.product_master_field` | The workbook's field dictionary |
| `mdm.vw_product_master` | Each product with its full hierarchy path |
| `publish.product_master_vw` | Same data under the workbook's column names, for downstream use |

Notes on the source data:

- `Product_Code` repeats in the workbook, so `product_id` is the key and `Product_Name` is the unique business key.
- When Model and/or Size are blank, the product hangs off the deepest level it has.
- RDHx under Air Cooling has a Size but no Model, so its Model shows as "(Not specified)".
- Leading and trailing spaces, including non-breaking spaces, are trimmed from hierarchy names.

To reload the data after the workbook changes:

```powershell
pip install openpyxl
python db/postgres/tools/generate_product_master_seed.py      # rewrites 008_product_master_seed.sql
psql -h localhost -U postgres -d aoc_mdm -v ON_ERROR_STOP=1 -f db\postgres\008_product_master_seed.sql
```

`Demo:ProductMasterOnly` in appsettings defaults to `true`. With it on, the menu shows only Product Master and it's the home page. Set it to `false` to show the spend hierarchy and the vendor and item menus again.

## Tech stack

- ASP.NET Core MVC on .NET 8, with server-rendered Razor views
- PostgreSQL, accessed through the PostgreSQL ODBC driver
- Python end-to-end tests that use only the standard library

## Project layout

| Path | Purpose |
|---|---|
| `AOC_MDM_MetaData_GUI` | Web app: controllers, views and startup |
| `AOC_MDM.Services` | Business logic and DTOs |
| `AOC_MDM.Infrastrucure` | Repositories and unit of work |
| `AOC_MDM.Core` | Entities and interfaces |
| `db/postgres` | Schema, seed data and publish view scripts |
| `tests` | End-to-end workflow tests |
| `EncryptDecrypt`, `TestDatabricks` | Small standalone console utilities |

## Roles

| Level | Access |
|---|---|
| 1 | View all data |
| 2 | View and submit changes for approval |
| 3 | Approve or reject changes, and edit the hierarchy |
| 4 | Everything, plus manage users |

## Column editing

Admins can lock individual columns under **Administration > Column Editing**. A locked column can't be changed by anyone, including Admins, through the Edit and Bulk Update pages, hierarchy Rename and Enable, or an approval. Pending requests approved after a lock keep the column's current value and still apply their other changes. Every column is editable until an Admin locks it.

## Getting started

You need the .NET 8 SDK, PostgreSQL and the PostgreSQL ODBC driver (Unicode).

1. Create a database named `aoc_mdm`, then run the scripts in `db/postgres`, in this order:

   ```powershell
   $env:PGPASSWORD = '<your postgres password>'
   $scripts = '001_aoc_mdm_schema.sql', '004_hierarchy_integrity_and_users.sql', '005_demo_users.sql', '006_column_edit_settings.sql',
              '002_seed_test_data.sql', '003_publish_views.sql', '007_product_master_schema.sql', '008_product_master_seed.sql'
   foreach ($s in $scripts) { psql -h localhost -U postgres -d aoc_mdm -v ON_ERROR_STOP=1 -f "db\postgres\$s" }
   ```

   `002` and `005` load test data and demo accounts, so don't run them in production.

2. Copy `AOC_MDM_MetaData_GUI/appsettings.example.json` to `appsettings.json` and `appsettings.Development.json` in the same folder, then fill in the `CHANGE_ME` values. These copies are git-ignored, so never commit real credentials.

3. Start the app:

   ```powershell
   cd AOC_MDM_MetaData_GUI
   dotnet run --launch-profile http
   ```

4. Open http://localhost:5081 and sign in. To list the demo accounts on the login page, set `Demo:ShowDemoAccounts` to `true` in `appsettings.Development.json`.

## Running the tests

`tests/run_e2e.ps1` resets and reseeds `aoc_mdm`, **which wipes its existing data**. It then builds and starts the app and runs every test phase. Port 5081 must be free, so stop any running copy first. The script expects `psql.exe` under `C:\Program Files\PostgreSQL\18\bin`, and Python must be on your `PATH`.

```powershell
$env:PGPASSWORD = '<your postgres password>'
powershell -ExecutionPolicy Bypass -File tests\run_e2e.ps1
```
