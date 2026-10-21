<#
.SYNOPSIS
    Creates the local ARIS database in the Docker SQL Server container, applies the
    canonical schema, and creates the least-privilege application login.

.DESCRIPTION
    One repeatable sequence for a local developer database. There is no cloud account,
    no firewall rule, and no shared state: each developer runs their own container.

    Steps
      1. Verify Docker, the schema file, and the SQL Server container.
      2. Read the sa password from the git-ignored .env file (or prompt for it).
      3. Wait until SQL Server accepts connections.
      4. Create the database ArisDb if it does not exist.
      5. Apply docs/aris_schema_v2.sql if the schema is not already present.
      6. Create the server login aris_officer, the database user, and add it to aris_app.
      7. Verify that aris_officer can connect, that UPDATE on AuditLog is denied,
         and that DELETE on a business table is denied.

    The script never writes a password into the repository, and it does not put the
    password on the host command line: sqlcmd reads SQLCMDPASSWORD from the container
    environment.

.PARAMETER ContainerName
    Docker container name. Default: aris-sqlserver (matches docker-compose.yml).

.PARAMETER Database
    Database name. Default: ArisDb.

.PARAMETER AdminLogin
    SQL Server administrative login used for deployment. Default: sa.

.PARAMETER AppUser
    Application login to create. Default: aris_officer. Must differ from the role name.

.PARAMETER AppRole
    Least-privilege database role created by the schema. Default: aris_app.

.PARAMETER AppPassword
    Password for the application login. If omitted, a strong one is generated and
    printed once when the login is created.

.PARAMETER SkipSchema
    Do not apply the schema (use when only the application login needs repair).

.PARAMETER SkipAppUser
    Do not create or verify the application login.

.EXAMPLE
    .\scripts\deploy-database.ps1

.EXAMPLE
    # Show the plan without starting a container or asking for a password
    .\scripts\deploy-database.ps1 -WhatIf

.EXAMPLE
    # Repair only the application login
    .\scripts\deploy-database.ps1 -SkipSchema
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $ContainerName = 'aris-sqlserver',
    [string] $Database      = 'ArisDb',
    [string] $AdminLogin    = 'sa',
    [string] $AppUser       = 'aris_officer',
    [string] $AppRole       = 'aris_app',
    [string] $AppPassword,
    [switch] $SkipSchema,
    [switch] $SkipAppUser
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Native commands report failures through $LASTEXITCODE and stderr. Do not turn
# sqlcmd's expected "permission denied" output into a terminating PowerShell error.
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

$repoRoot    = Split-Path -Parent $PSScriptRoot
$schemaFile  = Join-Path $repoRoot 'docs/aris_schema_v2.sql'
$composeFile = Join-Path $repoRoot 'docker-compose.yml'
$envFile     = Join-Path $repoRoot '.env'
$schemaInContainer = '/tmp/aris_schema_v2.sql'

function Write-Step { param([string]$Text) Write-Host "`n=== $Text" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Text) Write-Host "  [ok]   $Text" -ForegroundColor Green }
function Write-Warn { param([string]$Text) Write-Host "  [warn] $Text" -ForegroundColor Yellow }
function Write-Fail { param([string]$Text) Write-Host "  [FAIL] $Text" -ForegroundColor Red }

<#
    Runs a native command without letting stderr become a terminating PowerShell
    error. Native failures are reported through ExitCode, which is what this script
    needs (sqlcmd writes an expected permission error when the audit DENY is tested).
#>
function Invoke-Native {
    param(
        [Parameter(Mandatory)][string] $FilePath,
        [string[]] $Arguments = @()
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $FilePath @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }

    return [pscustomobject]@{ ExitCode = $exitCode; Output = @($output) }
}

function Test-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'docker not found. Install Docker Desktop and reopen the terminal.'
    }

    $probe = Invoke-Native docker @('info')
    if ($probe.ExitCode -ne 0) {
        throw 'Docker is not running. Start Docker Desktop and retry.'
    }
}

function Get-ContainerRunning {
    $probe = Invoke-Native docker @('inspect', '-f', '{{.State.Running}}', $ContainerName)
    if ($probe.ExitCode -ne 0) { return $false }
    return (($probe.Output -join '').Trim() -eq 'true')
}

function Resolve-SqlcmdPath {
    foreach ($candidate in @(
        '/opt/mssql-tools18/bin/sqlcmd',
        '/opt/mssql-tools/bin/sqlcmd'
    )) {
        $probe = Invoke-Native docker @('exec', $ContainerName, 'test', '-x', $candidate)
        if ($probe.ExitCode -eq 0) { return $candidate }
    }

    throw "sqlcmd was not found inside container '$ContainerName'. Is the image mcr.microsoft.com/mssql/server:2022-latest?"
}

function Read-EnvPassword {
    if (-not (Test-Path $envFile)) { return $null }

    $line = Get-Content $envFile |
        Where-Object { $_ -match '^\s*MSSQL_SA_PASSWORD\s*=' } |
        Select-Object -First 1

    if (-not $line) { return $null }

    $value = ($line -replace '^\s*MSSQL_SA_PASSWORD\s*=\s*', '').Trim()
    $value = $value.Trim('"').Trim("'")
    if ([string]::IsNullOrWhiteSpace($value)) { return $null }
    return $value
}

function Get-AdminPassword {
    $fromEnv = Read-EnvPassword
    if ($fromEnv) { return $fromEnv }

    Write-Warn "MSSQL_SA_PASSWORD was not found in $envFile."
    $secure = Read-Host -Prompt "SQL Server password for '$AdminLogin'" -AsSecureString
    $plain  = [System.Net.NetworkCredential]::new('', $secure).Password
    if ([string]::IsNullOrWhiteSpace($plain)) { throw 'No admin password supplied.' }
    return $plain
}

function New-StrongPassword {
    # Excludes $ and quotes so the value survives shells, .env files, and SQL literals.
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
        [string] $InputFile,
        [string] $TargetDatabase = $Database
    )

    $arguments = @(
        'exec', '-i',
        '-e', "SQLCMDPASSWORD=$Password",
        $ContainerName, $Sqlcmd,
        '-S', 'localhost', '-U', $Login, '-C', '-b', '-h', '-1', '-I'
    )

    if ($InputFile) {
        $arguments += @('-d', $TargetDatabase, '-i', $InputFile)
    }
    else {
        $arguments += @('-d', $TargetDatabase, '-Q', $Query)
    }

    $output = Invoke-Native docker $arguments
    return [pscustomobject]@{ ExitCode = $output.ExitCode; Output = @($output.Output) }
}

function Wait-SqlReady {
    param(
        [Parameter(Mandatory)][string] $Sqlcmd,
        [Parameter(Mandatory)][string] $Password
    )

    Write-Step "Waiting for SQL Server in '$ContainerName'"
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        $probe = Invoke-Sql -Sqlcmd $Sqlcmd -Login $AdminLogin -Password $Password `
            -TargetDatabase 'master' -Query 'SELECT 1;'
        if ($probe.ExitCode -eq 0) {
            Write-Ok 'SQL Server is accepting connections'
            return
        }
        Start-Sleep -Seconds 2
    }

    throw "SQL Server did not become ready within 120 seconds. Check: docker logs $ContainerName"
}

function Quote-SqlLiteral {
    param([Parameter(Mandatory)][string] $Value)
    return $Value.Replace("'", "''")
}

# ------------------------------------------------------------------- pre-flight
Write-Step 'Pre-flight'

Test-Docker
Write-Ok 'Docker is running'

if (-not (Test-Path $schemaFile)) { throw "Schema file not found: $schemaFile" }
Write-Ok "Schema file: $schemaFile"

$running = Get-ContainerRunning
if (-not $running) {
    if (-not (Test-Path $composeFile)) {
        throw "Container '$ContainerName' is not running and docker-compose.yml was not found."
    }

    if ($PSCmdlet.ShouldProcess($ContainerName, 'Start SQL Server container')) {
        Write-Step 'Starting SQL Server container'
        $compose = Invoke-Native docker @('compose', '-f', $composeFile, 'up', '-d')
        if ($compose.ExitCode -ne 0) {
            Write-Fail ($compose.Output -join "`n")
            throw 'docker compose up failed.'
        }
        $running = Get-ContainerRunning
        if (-not $running) { throw "Container '$ContainerName' did not start. Check: docker logs $ContainerName" }
        Write-Ok "Container '$ContainerName' is running"
    }
}
else {
    Write-Ok "Container '$ContainerName' is running"
}

if (-not $PSCmdlet.ShouldProcess("$ContainerName/$Database", 'Deploy schema and application login')) {
    Write-Step 'WhatIf - nothing was changed'
    Write-Host "  would apply  : $schemaFile"
    Write-Host "  database     : $Database"
    Write-Host "  would create : login '$AppUser' in role '$AppRole'"
    Write-Host "  would verify : connection as '$AppUser' and DENY on AuditLog"
    return
}

$sqlcmd = Resolve-SqlcmdPath
Write-Ok "sqlcmd: $sqlcmd"

$adminPassword = Get-AdminPassword
Wait-SqlReady -Sqlcmd $sqlcmd -Password $adminPassword

# --------------------------------------------------------------------- database
Write-Step "Ensuring database '$Database'"

$createDatabase = @"
IF DB_ID(N'$Database') IS NULL
BEGIN
    CREATE DATABASE [$Database];
    PRINT 'created database $Database';
END
ELSE
    PRINT 'database $Database already exists';
"@

$dbResult = Invoke-Sql -Sqlcmd $sqlcmd -Login $AdminLogin -Password $adminPassword `
    -TargetDatabase 'master' -Query $createDatabase
if ($dbResult.ExitCode -ne 0) {
    Write-Fail ($dbResult.Output -join "`n")
    throw "Could not create or access database '$Database'."
}
$dbResult.Output | Where-Object { $_ } | ForEach-Object { Write-Ok $_.ToString().Trim() }

# ----------------------------------------------------------------------- schema
$schemaPresent = $false
$schemaProbe = Invoke-Sql -Sqlcmd $sqlcmd -Login $AdminLogin -Password $adminPassword `
    -Query "SELECT CASE WHEN OBJECT_ID(N'dbo.Citizen', N'U') IS NULL THEN 0 ELSE 1 END;"
if ($schemaProbe.ExitCode -eq 0) {
    $schemaPresent = (($schemaProbe.Output -join '').Trim() -eq '1')
}

if ($SkipSchema) {
    Write-Step 'Schema: skipped (-SkipSchema)'
}
elseif ($schemaPresent) {
    Write-Step 'Schema: already present'
    Write-Warn 'Not re-applying the DDL. To rebuild, reset the local database:'
    Write-Host '           docker compose down -v; docker compose up -d; .\scripts\deploy-database.ps1' -ForegroundColor DarkGray
}
else {
    Write-Step 'Applying schema'
    $copied = Invoke-Native docker @('cp', $schemaFile, "${ContainerName}:$schemaInContainer")
    if ($copied.ExitCode -ne 0) {
        Write-Fail ($copied.Output -join "`n")
        throw 'Could not copy the schema into the container.'
    }

    $applied = Invoke-Sql -Sqlcmd $sqlcmd -Login $AdminLogin -Password $adminPassword `
        -InputFile $schemaInContainer
    if ($applied.ExitCode -ne 0) {
        Write-Fail ($applied.Output -join "`n")
        throw 'Schema deployment failed.'
    }
    Write-Ok 'Schema applied (tables, constraints, triggers, views, role aris_app, seed configuration)'
}

# ---------------------------------------------------------------------- app user
$generatedPassword = $false
$canVerify = $false
$connectionString = $null

if ($SkipAppUser) {
    Write-Step 'Application user: skipped (-SkipAppUser)'
}
else {
    # Determine whether the login already exists before generating a password.
    $loginProbe = Invoke-Sql -Sqlcmd $sqlcmd -Login $AdminLogin -Password $adminPassword `
        -TargetDatabase 'master' `
        -Query "SELECT CASE WHEN SUSER_ID(N'$AppUser') IS NULL THEN 0 ELSE 1 END;"
    if ($loginProbe.ExitCode -ne 0) {
        Write-Fail ($loginProbe.Output -join "`n")
        throw "Could not inspect login '$AppUser'."
    }
    $loginExists = (($loginProbe.Output -join '').Trim() -eq '1')
    $resetPassword = -not [string]::IsNullOrWhiteSpace($AppPassword)

    $loginSql = $null
    if (-not $loginExists) {
        if (-not $resetPassword) {
            $AppPassword = New-StrongPassword
            $generatedPassword = $true
        }
        $escapedAppPassword = Quote-SqlLiteral $AppPassword
        $loginSql = @"
CREATE LOGIN [$AppUser] WITH PASSWORD = N'$escapedAppPassword', CHECK_POLICY = ON;
PRINT 'created login $AppUser';
"@
        $canVerify = $true
    }
    elseif ($resetPassword) {
        $escapedAppPassword = Quote-SqlLiteral $AppPassword
        $loginSql = @"
ALTER LOGIN [$AppUser] WITH PASSWORD = N'$escapedAppPassword';
PRINT 'reset password for login $AppUser';
"@
        $canVerify = $true
    }
    else {
        Write-Warn "Login '$AppUser' already exists; its password was not changed."
        Write-Host '           Pass -AppPassword to reset it. Skipping the connection check.' -ForegroundColor DarkGray
    }

    if ($loginSql) {
        Write-Step "Ensuring application login '$AppUser'"
        $loginResult = Invoke-Sql -Sqlcmd $sqlcmd -Login $AdminLogin -Password $adminPassword `
            -TargetDatabase 'master' -Query $loginSql
        if ($loginResult.ExitCode -ne 0) {
            Write-Fail ($loginResult.Output -join "`n")
            throw "Could not create or update login '$AppUser'."
        }
        $loginResult.Output | Where-Object { $_ } | ForEach-Object { Write-Ok $_.ToString().Trim() }
    }

    $createUser = @"
IF DATABASE_PRINCIPAL_ID(N'$AppUser') IS NULL
BEGIN
    CREATE USER [$AppUser] FOR LOGIN [$AppUser];
    PRINT 'created database user $AppUser';
END
ELSE
    PRINT 'database user $AppUser already exists';

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
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

    $userResult = Invoke-Sql -Sqlcmd $sqlcmd -Login $AdminLogin -Password $adminPassword `
        -Query $createUser
    if ($userResult.ExitCode -ne 0) {
        Write-Fail ($userResult.Output -join "`n")
        throw "Could not create database user '$AppUser' or add it to '$AppRole'."
    }
    $userResult.Output | Where-Object { $_ } | ForEach-Object { Write-Ok $_.ToString().Trim() }

    # ----------------------------------------------------------------- verification
    if ($canVerify) {
        Write-Step "Verifying '$AppUser'"

        $connectProbe = Invoke-Sql -Sqlcmd $sqlcmd -Login $AppUser -Password $AppPassword `
            -Query 'SELECT 1;'
        if ($connectProbe.ExitCode -eq 0) {
            Write-Ok "connects as '$AppUser'"
        }
        else {
            Write-Fail ($connectProbe.Output -join "`n")
            throw "The application login could not connect. Re-run with -AppPassword to reset the login password."
        }

        # The append-only guarantee: aris_app has DENY UPDATE on AuditLog, so this MUST fail.
        $denied = Invoke-Sql -Sqlcmd $sqlcmd -Login $AppUser -Password $AppPassword `
            -Query 'UPDATE dbo.AuditLog SET Result = Result;'
        if ($denied.ExitCode -eq 0) {
            Write-Fail 'UPDATE on AuditLog SUCCEEDED - the DENY rule is not in force.'
        }
        else {
            Write-Ok 'UPDATE on AuditLog correctly denied (append-only holds)'
        }

        # History is never deleted: DENY DELETE on the schema must reject this.
        $deleteDenied = Invoke-Sql -Sqlcmd $sqlcmd -Login $AppUser -Password $AppPassword `
            -Query 'DELETE FROM dbo.Citizen WHERE 1 = 0;'
        if ($deleteDenied.ExitCode -eq 0) {
            Write-Fail 'DELETE on dbo.Citizen SUCCEEDED - the DENY DELETE rule is not in force.'
        }
        else {
            Write-Ok 'DELETE on dbo.Citizen correctly denied'
        }

        $connectionString = "Server=localhost,1433;Database=$Database;User Id=$AppUser;Password=$AppPassword;TrustServerCertificate=True;Connect Timeout=30;"
    }

    Write-Step 'Done'

    if ($connectionString) {
        Write-Host @"

Next steps:
  1. Run the integration suite against this database:
       `$env:ARIS_TEST_CONNECTION = "Server=localhost,1433;Database=$Database;User Id=sa;Password=<from .env>;TrustServerCertificate=True"
       dotnet test Aris.slnx

  2. Point the WinForms app at the database (secret stays outside Git):
       cd src/Aris.UI
       dotnet user-secrets set "ConnectionStrings:ArisDb" "$connectionString"
       cd ../..

  3. Reset everything (destroys local data):
       docker compose down -v
       docker compose up -d
       .\scripts\deploy-database.ps1
"@ -ForegroundColor Gray

        if ($generatedPassword) {
            Write-Host '  Generated password for the application login (shown once - record it now):' -ForegroundColor Yellow
            Write-Host "    $AppPassword" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host @"

The database and application login are ready. The login already existed, so its
password was left unchanged. Use the password already stored in user-secrets, or
re-run with -AppPassword to reset it.

Reset everything (destroys local data):
  docker compose down -v
  docker compose up -d
  .\scripts\deploy-database.ps1
"@ -ForegroundColor Gray
    }
}
