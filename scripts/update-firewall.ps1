<#
.SYNOPSIS
    Allows this machine's current public IP through the Azure SQL firewall.

.DESCRIPTION
    Azure SQL rejects every client IP by default, and a developer's public IP changes
    whenever they switch networks (home, hall, tethering, VPN). Instead of editing
    the rule by hand in the Azure Portal every time, this script:

      1. Finds this machine's current public IPv4 address.
      2. Ensures the Azure CLI is installed and signed in (runs 'az login' if needed).
      3. Creates or updates a PER-USER firewall rule named after your Windows
         account, so teammates never overwrite each other's rule.
      4. Verifies the rule now contains your address.

    Nothing is written into the repository, and no password is ever passed on the
    command line.

    Requirements
      - Azure CLI:  winget install Microsoft.AzureCLI   (reopen the terminal after)
      - An Azure account that may write the SQL server's firewall rules
        (Owner / Contributor, or SQL Server Contributor on the resource group).

.PARAMETER ResourceGroup
    Resource group that holds the SQL server. Default: ARIS-RG.

.PARAMETER Server
    Azure SQL logical server name, with or without the ".database.windows.net"
    suffix. Default: aris-demo-b1805.

.PARAMETER Database
    Only used in the closing connection-string hint. Default: ARIS-DB.

.PARAMETER Rule
    Firewall rule name. Defaults to "dev-<windows-username>" so each developer gets
    a rule of their own.

.PARAMETER Ip
    Skip detection and use this IPv4 address instead. Handy for testing.

.PARAMETER Subscription
    Optional subscription id or name to select before making changes.

.EXAMPLE
    .\scripts\update-firewall.ps1

.EXAMPLE
    # Show what would change without signing in or touching Azure
    .\scripts\update-firewall.ps1 -WhatIf

.EXAMPLE
    .\scripts\update-firewall.ps1 -Rule dev-bryan -Subscription 'My Subscription'
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $ResourceGroup = 'ARIS-RG',
    [string] $Server        = 'aris-demo-b1805',
    [string] $Database      = 'ARIS-DB',
    [string] $Rule,
    [string] $Ip,
    [string] $Subscription
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step { param([string]$Text) Write-Host "`n=== $Text" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Text) Write-Host "  [ok]   $Text" -ForegroundColor Green }
function Write-Warn { param([string]$Text) Write-Host "  [warn] $Text" -ForegroundColor Yellow }
function Write-Fail { param([string]$Text) Write-Host "  [FAIL] $Text" -ForegroundColor Red }

function Test-IsIPv4 {
    param([string]$Value)
    $parsed = $null
    return [System.Net.IPAddress]::TryParse($Value, [ref]$parsed) -and
           $parsed.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork
}

# Multiple endpoints so one outage does not break the script.
function Get-PublicIPv4 {
    $endpoints = @(
        'https://ipv4.icanhazip.com',
        'https://api.ipify.org',
        'https://ifconfig.me/ip'
    )
    foreach ($url in $endpoints) {
        try {
            $value = (Invoke-RestMethod -Uri $url -TimeoutSec 15).ToString().Trim()
            if (Test-IsIPv4 $value) { return $value }
        }
        catch {
            Write-Warn "Could not read the public IP from $url"
        }
    }
    throw 'Could not determine this machine''s public IPv4 address. Check your network, or pass -Ip <address>.'
}

<#
    On Windows PowerShell 5.1 a native command that writes to stderr becomes a
    TERMINATING error when $ErrorActionPreference is 'Stop', so $LASTEXITCODE never
    gets read. Lower the preference for the duration of the call and read the exit
    code ourselves instead.
#>
function Invoke-AzCli {
    param(
        [Parameter(Mandatory)][string]   $Az,
        [Parameter(Mandatory)][string[]] $Arguments
    )
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $Az @Arguments 2>&1
        $code   = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $saved
    }
    return [pscustomobject]@{ ExitCode = $code; Output = @($output) }
}

# Same, but lets 'az' talk to the console directly (interactive login shows a code).
function Invoke-AzCliInteractive {
    param(
        [Parameter(Mandatory)][string]   $Az,
        [Parameter(Mandatory)][string[]] $Arguments
    )
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $Az @Arguments
        return $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $saved
    }
}

# az -o json, parsed. Returns $null when there is nothing valid to parse.
function ConvertFrom-AzJson {
    param([string[]] $Output)

    $text = ($Output |
        Where-Object { $_ -and $_.ToString().Trim() } |
        ForEach-Object { $_.ToString() }) -join "`n"
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    try { return ($text | ConvertFrom-Json) }
    catch { return $null }
}

# StrictMode-safe property read on a parsed az object.
function Get-AzValue {
    param($Object, [string] $Name)

    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($property) { return $property.Value }
    return $null
}

<#
    Entra ID can refuse to mint a token even when 'az account show' works from cache.
    The classic example is AADSTS530035 ("Access has been blocked by security
    defaults"), which is an identity/tenant policy block - not a permission or script
    problem. Azure prints the exact re-login command; surface it verbatim.
#>
function Show-AuthBlockHint {
    param([string[]] $Output)

    $text = $Output -join "`n"
    if ($text -notmatch 'AADSTS|Access has been blocked') { return $false }

    $tenant = ''
    if ($text -match 'tenant\s+"([0-9a-fA-F-]{36})"') { $tenant = $Matches[1] }

    Write-Host ''
    Write-Fail 'Azure refused to issue an access token for this account (AADSTS).'
    Write-Host '  This is an identity / tenant policy block, not a permission or script bug.' -ForegroundColor Yellow
    Write-Host '  Sign in again with the tenant and scope Azure asked for:' -ForegroundColor Yellow
    Write-Host ''
    if ($tenant) {
        Write-Host '    az logout' -ForegroundColor White
        Write-Host "    az login --tenant `"$tenant`" --scope `"https://management.core.windows.net//.default`"" -ForegroundColor White
    }
    else {
        Write-Host '    az logout' -ForegroundColor White
        Write-Host '    az login' -ForegroundColor White
    }
    Write-Host ''
    Write-Host '  Then re-run this script. The account owner may need to complete MFA.' -ForegroundColor DarkGray
    return $true
}

# ------------------------------------------------------------- resolve inputs
# A rule name is 1-128 chars of letters, digits, '-', '_' and '.'.
if ([string]::IsNullOrWhiteSpace($Rule)) {
    $who = if ($env:USERNAME) { $env:USERNAME } else { 'developer' }
    $slug = $who.ToLowerInvariant() -replace '[^a-z0-9._-]', '-'
    $slug = ($slug -replace '-{2,}', '-').Trim('-', '.')
    if ([string]::IsNullOrEmpty($slug)) { $slug = 'developer' }
    $Rule = "dev-$slug"
}

$ServerName = $Server.Trim()
$ServerName = $ServerName -replace '\.database\.windows\.net\.?$', ''

if (-not [string]::IsNullOrWhiteSpace($Ip)) {
    if (-not (Test-IsIPv4 $Ip)) { throw "'$Ip' is not a valid IPv4 address." }
}
else {
    $Ip = Get-PublicIPv4
}

# ------------------------------------------------------------------ pre-flight
Write-Step 'Pre-flight'
Write-Host "  server           : $ServerName (resource group: $ResourceGroup)"
Write-Host "  database         : $Database"
Write-Host "  firewall rule    : $Rule"
Write-Host "  current public IP: $Ip"

# -WhatIf: report the plan and stop before touching Azure or signing in.
if (-not $PSCmdlet.ShouldProcess("$ServerName/$Rule", "Allow public IP $Ip")) {
    Write-Step 'WhatIf - nothing was changed'
    Write-Host "  would create or update firewall rule '$Rule' to $Ip"
    return
}

$AzCommand = Get-Command az -ErrorAction SilentlyContinue
if (-not $AzCommand) {
    throw 'Azure CLI (az) not found. Install it with: winget install Microsoft.AzureCLI  (then reopen your terminal).'
}
$Az = $AzCommand.Source
Write-Ok "az: $Az"

# Reuse the existing session; only prompt for a sign-in when there is none.
$account = Invoke-AzCli -Az $Az -Arguments @('account', 'show', '--query', 'id', '-o', 'tsv', '--only-show-errors')
if ($account.ExitCode -ne 0) {
    Write-Warn 'Not signed in to Azure CLI - starting az login (a browser window will open)...'
    $loginCode = Invoke-AzCliInteractive -Az $Az -Arguments @('login', '--only-show-errors')
    if ($loginCode -ne 0) { throw 'az login failed.' }
}

if (-not [string]::IsNullOrWhiteSpace($Subscription)) {
    $setSub = Invoke-AzCli -Az $Az -Arguments @('account', 'set', '--subscription', $Subscription, '--only-show-errors')
    if ($setSub.ExitCode -ne 0) {
        if (Show-AuthBlockHint -Output $setSub.Output) { exit 1 }
        Write-Fail ($setSub.Output -join "`n")
        throw "Could not select subscription '$Subscription'."
    }
    Write-Ok "subscription: $Subscription"
}

$context = Invoke-AzCli -Az $Az -Arguments @('account', 'show', '--query', 'name', '-o', 'tsv', '--only-show-errors')
if ($context.ExitCode -eq 0 -and $context.Output.Count -gt 0) {
    Write-Ok "subscription: $($context.Output[0].ToString().Trim())"
}

# Are we already correct? show returns non-zero when the rule does not exist.
$showArgs = @(
    'sql', 'server', 'firewall-rule', 'show',
    '--resource-group', $ResourceGroup,
    '--server',         $ServerName,
    '--name',           $Rule,
    '-o', 'json', '--only-show-errors'
)
$existing     = Invoke-AzCli -Az $Az -Arguments $showArgs
$existingRule = $null
if ($existing.ExitCode -eq 0) {
    $existingRule = ConvertFrom-AzJson -Output $existing.Output
}

if ($null -ne $existingRule) {
    if ((Get-AzValue $existingRule 'startIpAddress') -eq $Ip -and
        (Get-AzValue $existingRule 'endIpAddress')   -eq $Ip) {
        Write-Step 'Done'
        Write-Ok "rule '$Rule' already allows $Ip - nothing to change."
        return
    }
    Write-Step "Updating rule '$Rule'"
    $action   = 'update'
    $verbArgs = @(
        'sql', 'server', 'firewall-rule', 'update',
        '--resource-group', $ResourceGroup,
        '--server',         $ServerName,
        '--name',           $Rule,
        '--start-ip-address', $Ip,
        '--end-ip-address',   $Ip,
        '-o', 'none', '--only-show-errors'
    )
}
else {
    # Distinguish "rule not found" from an authentication block before creating.
    if (Show-AuthBlockHint -Output $existing.Output) { exit 1 }

    Write-Step "Creating rule '$Rule'"
    $action   = 'create'
    $verbArgs = @(
        'sql', 'server', 'firewall-rule', 'create',
        '--resource-group', $ResourceGroup,
        '--server',         $ServerName,
        '--name',           $Rule,
        '--start-ip-address', $Ip,
        '--end-ip-address',   $Ip,
        '-o', 'none', '--only-show-errors'
    )
}

$result = Invoke-AzCli -Az $Az -Arguments $verbArgs
if ($result.ExitCode -ne 0) {
    if (Show-AuthBlockHint -Output $result.Output) { exit 1 }
    Write-Fail ($result.Output -join "`n")
    throw "Could not $action firewall rule '$Rule'. You may lack permission on the SQL server or resource group, or the resource group/server name is wrong."
}
Write-Ok "rule '$Rule' $action`d"

# ------------------------------------------------------------------- verify
$verify = Invoke-AzCli -Az $Az -Arguments $showArgs
if ($verify.ExitCode -ne 0) {
    if (Show-AuthBlockHint -Output $verify.Output) { exit 1 }
    throw "Rule '$Rule' was written but could not be re-read."
}

$vRule = ConvertFrom-AzJson -Output $verify.Output
if ($null -ne $vRule -and
    (Get-AzValue $vRule 'startIpAddress') -eq $Ip -and
    (Get-AzValue $vRule 'endIpAddress')   -eq $Ip) {
    Write-Ok "verified: $ServerName/'$Rule' now allows $Ip"
}
else {
    $shown = if ($vRule) { "$(Get-AzValue $vRule 'startIpAddress')-$(Get-AzValue $vRule 'endIpAddress')" } else { ($verify.Output -join ' ') }
    Write-Fail "verification mismatch: rule is now '$shown' (expected $Ip)."
    throw 'The firewall rule did not end up with the expected address.'
}

Write-Step 'Done'
Write-Host @"
  Your IP $Ip is allowed on '$ServerName'. Azure can take up to 5 minutes to apply
  a firewall change, so if a connection still fails, wait a moment and retry.

  Connection string:
    Server=tcp:$ServerName.database.windows.net,1433;Database=$Database;User Id=aris_officer;Password=<password>;Encrypt=True;Connect Timeout=60;

  If your organisation routes traffic through a proxy, the IP Azure sees may differ
  from the one detected here. In that case re-run with -Ip <the address from the error>.
"@ -ForegroundColor Gray
