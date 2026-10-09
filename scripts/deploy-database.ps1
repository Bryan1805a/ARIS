<#
.SYNOPSIS
    Deploys the ARIS schema to Azure SQL Database and creates the application user.

.DESCRIPTION
    One repeatable sequence so every teammate can get a working database without
    hand-typing sqlcmd commands. Safe to re-run: every step checks for existence first.

    Steps
      1. Confirm sqlcmd is installed, the schema file exists, and the server is reachable.
      2. Apply docs/aris_schema_v2.sql (tables, views, triggers, and the
         least-privilege role aris_app).
      3. Create the application user and add it to that role.
      4. Verify the user can connect, and that the append-only DENY rules hold.

    The server admin password is prompted for and never passed on the command line,
    so it does not land in your shell history. Nothing is written into the repo.

.PARAMETER Server
    Azure SQL logical server hostname. Not a secret - it still requires credentials.

.PARAMETER AdminLogin
    Server admin login, used only for setup.

.PARAMETER AppUser
    Team login to create. Must differ from the role name "aris_app".

.PARAMETER AppPassword
    Password for the team login. If omitted, a strong one is generated and printed once.

.EXAMPLE
    .\scripts\deploy-database.ps1

.EXAMPLE
    # Show the plan without touching the database or prompting for a password
    .\scripts\deploy-database.ps1 -WhatIf

.EXAMPLE
    # Verify an existing setup only
    .\scripts\deploy-database.ps1 -SkipSchema -SkipAppUser
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Server      = 'aris-demo-b1805.database.windows.net',
    [string] $Database    = 'ARIS-DB',
    [string] $AdminLogin  = 'Bryan',
    [string] $AppUser     = 'aris_officer',
    [string] $AppRole     = 'aris_app',
    [string] $AppPassword,
    [switch] $SkipSchema,
    [switch] $SkipAppUser
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot   = Split-Path -Parent $PSScriptRoot
$schemaFile = Join-Path $repoRoot 'docs/aris_schema_v2.sql'

function Write-Step { param([string]$Text) Write-Host "`n=== $Text" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Text) Write-Host "  [ok]   $Text" -ForegroundColor Green }
function Write-Warn { param([string]$Text) Write-Host "  [warn] $Text" -ForegroundColor Yellow }
function Write-Fail { param([string]$Text) Write-Host "  [FAIL] $Text" -ForegroundColor Red }

function Find-Sqlcmd {
    $onPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    $fallback = 'C:\Program Files\SqlCmd\sqlcmd.exe'
    if (Test-Path $fallback) { return $fallback }

    throw 'sqlcmd not found. Install it with: winget install Microsoft.Sqlcmd'
}

function New-StrongPassword {
    # Excludes $ and quotes so the value survives .env files, shells and SQL literals.
    $chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!#+,-.:=?@_'.ToCharArray()
    $bytes = New-Object byte[] 28
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] })
}

function Invoke-Sql {
    param(
        [Parameter(Mandatory)][string] $Sqlcmd,
        [Parameter(Mandatory)][string] $Login,
        [Parameter(Mandatory)][string] $Password,
        [string] $Query,
        [string] $InputFile
    )

    $arguments = @('-S', $Server, '-d', $Database, '-U', $Login, '-P', $Password, '-N', '-C', '-b', '-h', '-1')
    if ($InputFile) { $arguments += @('-i', $InputFile) } else { $arguments += @('-Q', $Query) }

    $output = & $Sqlcmd @arguments 2>&1
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = @($output) }
}

function Get-ConnectionString {
    param([string]$Login, [string]$Password)
    "Server=tcp:$Server,1433;Database=$Database;User Id=$Login;Password=$Password;Encrypt=True;TrustServerCertificate=False;Connect Timeout=60;"
}

# ---------------------------------------------------------------- pre-flight
Write-Step 'Pre-flight'

$Sqlcmd = Find-Sqlcmd
Write-Ok "sqlcmd: $Sqlcmd"

if (-not (Test-Path $schemaFile)) { throw "Schema file not found: $schemaFile" }
Write-Ok "schema: $schemaFile"

$tcp = Test-NetConnection -ComputerName $Server -Port 1433 -WarningAction SilentlyContinue
if (-not $tcp.TcpTestSucceeded) {
    throw "Cannot reach $Server on port 1433. Add your client IP under the server's Networking page in the Azure portal."
}
Write-Ok "reachable: $Server port 1433"

# -WhatIf: report the plan and stop before asking for any secret.
if (-not $PSCmdlet.ShouldProcess("$Server/$Database", 'Deploy schema and application user')) {
    Write-Step 'WhatIf - nothing was changed'
    Write-Host "  would apply : $schemaFile"
    Write-Host "  would create: login '$AppUser' in role '$AppRole'"
    Write-Host "  would verify: connection as '$AppUser'"
    return
}

$secureAdmin   = Read-Host -Prompt "Server admin password for '$AdminLogin'" -AsSecureString
$adminPassword = [System.Net.NetworkCredential]::new('', $secureAdmin).Password
if ([string]::IsNullOrWhiteSpace($adminPassword)) { throw 'No admin password supplied.' }

$probe = Invoke-Sql -Sqlcmd $Sqlcmd -Login $AdminLogin -Password $adminPassword `
    -Query "SELECT DB_NAME(); SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128));"
if ($probe.ExitCode -ne 0) {
    Write-Fail ($probe.Output -join "`n")
    throw "Could not connect to '$Database' as '$AdminLogin'. Check the database name, login and password."
}
$probe.Output | Where-Object { $_ -and $_ -notmatch '^\s*$' } | ForEach-Object { Write-Ok $_.ToString().Trim() }

# ------------------------------------------------------------------- schema
if ($SkipSchema) {
    Write-Step 'Schema: skipped (-SkipSchema)'
}
else {
    Write-Step 'Applying schema'
    $applied = Invoke-Sql -Sqlcmd $Sqlcmd -Login $AdminLogin -Password $adminPassword -InputFile $schemaFile
    if ($applied.ExitCode -ne 0) {
        Write-Fail ($applied.Output -join "`n")
        throw 'Schema deployment failed.'
    }
    Write-Ok 'schema applied (tables, views, triggers, role aris_app)'
}

# --------------------------------------------------------------- app user
$generatedPassword = $false
if ($SkipAppUser) {
    Write-Step 'Application user: skipped (-SkipAppUser)'
}
else {
    if ([string]::IsNullOrWhiteSpace($AppPassword)) {
        $AppPassword = New-StrongPassword
        $generatedPassword = $true
    }

    # CREATE USER cannot be parameterised, so the password is an escaped literal.
    $escapedPassword = $AppPassword.Replace("'", "''")
    $createUser = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$AppUser')
BEGIN
    CREATE USER [$AppUser] WITH PASSWORD = N'$escapedPassword';
    PRINT 'created user $AppUser';
END
ELSE
    PRINT 'user $AppUser already exists';
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'$AppRole' AND m.name = N'$AppUser')
BEGIN
    ALTER ROLE [$AppRole] ADD MEMBER [$AppUser];
    PRINT 'added $AppUser to $AppRole';
END
ELSE
    PRINT '$AppUser is already a member of $AppRole';
"@

    Write-Step "Creating application user '$AppUser'"
    $created = Invoke-Sql -Sqlcmd $Sqlcmd -Login $AdminLogin -Password $adminPassword -Query $createUser
    if ($created.ExitCode -ne 0) {
        Write-Fail ($created.Output -join "`n")
        throw 'Could not create the application user.'
    }
    $created.Output | Where-Object { $_ -match 'created|added|already' } | ForEach-Object { Write-Ok $_.ToString().Trim() }
}

# ------------------------------------------------------------- verification
Write-Step "Verifying '$AppUser'"

# 1. Can the application user connect at all?
$connectProbe = Invoke-Sql -Sqlcmd $Sqlcmd -Login $AppUser -Password $AppPassword -Query 'SELECT 1;'
if ($connectProbe.ExitCode -eq 0) {
    Write-Ok "connects as '$AppUser'"
}
else {
    Write-Fail ($connectProbe.Output -join "`n")
    throw "The application user could not connect. Check the password and that the user is a member of '$AppRole'."
}

# 2. The append-only guarantee: aris_app has DENY UPDATE on AuditLog, so this MUST fail.
$denied = Invoke-Sql -Sqlcmd $Sqlcmd -Login $AppUser -Password $AppPassword -Query 'UPDATE dbo.AuditLog SET Result = Result;'
if ($denied.ExitCode -eq 0) {
    Write-Fail 'UPDATE on AuditLog SUCCEEDED - the DENY rule is not in force (BR-AUD-02 is not enforced).'
}
else {
    Write-Ok 'UPDATE on AuditLog correctly denied (append-only holds)'
}

$connectionString = Get-ConnectionString -Login $AppUser -Password $AppPassword

Write-Step 'Done'
Write-Host @"

Next steps:
  1. Run the full suite against this database:
       `$env:ARIS_TEST_CONNECTION = "$connectionString"
       dotnet test Aris.slnx
     Expected: 17 total, 0 failed, 0 skipped.

  2. Point the WinForms app at it permanently (stored outside the repo):
       cd src/Aris.UI
       dotnet user-secrets init
       dotnet user-secrets set "ConnectionStrings:ArisDb" "<the connection string>"

  3. Give teammates the '$AppUser' password - never the server admin password.
"@ -ForegroundColor Gray

if ($generatedPassword) {
    Write-Host "  Generated password for '$AppUser' (shown once - record it now):" -ForegroundColor Yellow
    Write-Host "    $AppPassword" -ForegroundColor Yellow
}
