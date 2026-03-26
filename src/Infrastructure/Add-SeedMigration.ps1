param(
    [string]$MigrationName = "SeedMasterDataFromScriptSql",
    [string]$Context = "AuthenticationDbContext",
    [string]$OutputDir = "Migrations",
    [string]$EnvironmentName = "Development"
)

$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

$appSettingsPath = Join-Path $PSScriptRoot "..\API\appsettings.$EnvironmentName.json"
if (Test-Path $appSettingsPath) {
    $env:POSTGRES_CONNECTIONSTRING = (Get-Content $appSettingsPath | ConvertFrom-Json).POSTGRES_CONNECTIONSTRING
}

$env:ASPNETCORE_ENVIRONMENT = $EnvironmentName

$existingMigrationFiles = Get-ChildItem -Path $OutputDir -Filter "*.cs" -File |
    Where-Object { $_.BaseName -match "_" } |
    ForEach-Object { $_.BaseName.Split("_", 2)[1] }

$existingVersionedMigrations = Get-ChildItem -Path $OutputDir -Filter "*.cs" -File |
    Where-Object {
        $_.Name -notlike "*.Designer.cs" -and
        $_.BaseName -match "^\d{14}_"
    }

if ($existingVersionedMigrations.Count -eq 0) {
    throw "No versioned migrations were found in '$OutputDir'. Create InitialCreate first, then run .\Add-SeedMigration.ps1."
}

$resolvedMigrationName = $MigrationName
if ($existingMigrationFiles -contains $MigrationName) {
    $resolvedMigrationName = "{0}_{1}" -f $MigrationName, (Get-Date -Format "yyyyMMddHHmmss")
}

Write-Host "Creating migration: $resolvedMigrationName"

$beforeFiles = Get-ChildItem -Path $OutputDir -Filter "*.cs" -File | Select-Object -ExpandProperty FullName
dotnet ef migrations add $resolvedMigrationName --context $Context --output-dir $OutputDir

$newMigrationFile = Get-ChildItem -Path $OutputDir -Filter "*.cs" -File |
    Where-Object {
        $_.FullName -notin $beforeFiles -and
        $_.Name -notlike "*.Designer.cs" -and
        $_.BaseName -like "*_$resolvedMigrationName"
    } |
    Select-Object -First 1

if ($null -eq $newMigrationFile) {
    throw "Unable to locate the generated migration file for '$resolvedMigrationName'."
}

$migrationContent = Get-Content $newMigrationFile.FullName -Raw
$updatedContent = $migrationContent -replace "protected override void Up\(MigrationBuilder migrationBuilder\)\s*\{\s*\}", "protected override void Up(MigrationBuilder migrationBuilder)`r`n    {`r`n        SeedMasterDataMigrationSupport.Apply(migrationBuilder);`r`n    }"
$updatedContent = $updatedContent -replace "protected override void Down\(MigrationBuilder migrationBuilder\)\s*\{\s*\}", "protected override void Down(MigrationBuilder migrationBuilder)`r`n    {`r`n        // Seed data should not be deleted automatically because the SQL is idempotent.`r`n    }"

if ($updatedContent -eq $migrationContent) {
    throw "Generated migration file '$($newMigrationFile.Name)' did not match the expected EF migration template."
}

Set-Content -Path $newMigrationFile.FullName -Value $updatedContent
Write-Host "Patched seed migration: $($newMigrationFile.Name)"