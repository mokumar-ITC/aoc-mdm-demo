# Runs the full AOC MDM workflow test against a fresh copy of the test data.
#   1. apply schema + reset + seed aoc_mdm   2. build   3. start the app once   4. run every phase (each signs in as its demo user)
# Usage (from the repo root):  powershell -ExecutionPolicy Bypass -File tests\run_e2e.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
if (-not $env:PGPASSWORD) { throw "Set the PGPASSWORD environment variable to the local postgres password first" }

Write-Host "== Reset and seed aoc_mdm" -ForegroundColor Cyan
$scripts = '001_aoc_mdm_schema.sql', '004_hierarchy_integrity_and_users.sql', '005_demo_users.sql', '006_column_edit_settings.sql',
           'reset_test_data.sql', '002_seed_test_data.sql', '003_publish_views.sql',
           '007_product_master_schema.sql', '008_product_master_seed.sql'
$args = @('-h', 'localhost', '-U', 'postgres', '-d', 'aoc_mdm', '-q', '-v', 'ON_ERROR_STOP=1')
foreach ($s in $scripts) { $args += @('-f', "$root\db\postgres\$s") }
& $psql @args 2>$null
if ($LASTEXITCODE -ne 0) { throw "Database reset failed" }

Write-Host "== Build" -ForegroundColor Cyan
dotnet build "$root\AOC_MDM_MetaData_GUI\AOC_MDM_MetaData_GUI.csproj" -nologo -v q | Select-String 'Error\(s\)'

Write-Host "== Start app" -ForegroundColor Cyan
# The tests use the vendor / item menus, which the Gradian product-master-only demo mode hides
$env:Demo__ProductMasterOnly = 'false'
$app = Start-Process dotnet -ArgumentList 'run', '--no-build', '--launch-profile', 'http' `
    -WorkingDirectory "$root\AOC_MDM_MetaData_GUI" -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput "$env:TEMP\aoc_mdm_e2e.log" -RedirectStandardError "$env:TEMP\aoc_mdm_e2e.err"
$failed = 0
try {
    $ready = $false
    for ($i = 0; $i -lt 60 -and -not $ready; $i++) {
        try { Invoke-WebRequest http://localhost:5081/Account/Login -UseBasicParsing -TimeoutSec 2 | Out-Null; $ready = $true } catch { Start-Sleep 1 }
    }
    if (-not $ready) { throw "App did not start - see $env:TEMP\aoc_mdm_e2e.log" }

    # order matters: hierarchy runs last because it renames / disables nodes
    foreach ($phase in 'smoke', 'access', 'admin', 'maker', 'checker', 'columns', 'hierarchy') {
        python "$root\tests\e2e_workflow.py" $phase
        if ($LASTEXITCODE -ne 0) { $failed++ }
    }
    # Gradian product master: maker / checker workflow
    python "$root\tests\e2e_product_master.py" http://localhost:5081 2
    if ($LASTEXITCODE -ne 0) { $failed++ }
}
finally {
    Get-Process AOC_MDM_MetaData_GUI -ErrorAction SilentlyContinue | Stop-Process -Force
    Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue
}
Write-Host ("`n== RESULT: " + $(if ($failed -eq 0) { 'ALL PHASES PASSED' } else { "$failed phase(s) FAILED" })) `
    -ForegroundColor $(if ($failed -eq 0) { 'Green' } else { 'Red' })
exit $failed
