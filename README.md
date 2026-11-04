# AOC MDM

A master data management (MDM) web app for maintaining vendor, item and item-supplier reference data, along with the spend hierarchy they belong to. Changes go through a maker/checker approval step, and approved data is published to downstream systems through read-only views.

The hierarchy is: Spend Area → Team → Level 1 → Level 2 → Level 3 → vendors and items.

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

## Getting started

You need the .NET 8 SDK, PostgreSQL and the PostgreSQL ODBC driver (Unicode).

1. Create a database named `aoc_mdm`, then run the scripts in `db/postgres`, in this order:

   ```powershell
   $env:PGPASSWORD = '<your postgres password>'
   $scripts = '001_aoc_mdm_schema.sql', '004_hierarchy_integrity_and_users.sql', '005_demo_users.sql',
              '002_seed_test_data.sql', '003_publish_views.sql'
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
