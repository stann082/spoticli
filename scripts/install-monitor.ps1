<#
.SYNOPSIS
    Installs the spoticli top-list monitor as a daily Scheduled Task.

.DESCRIPTION
    Publishes the monitor project as a single self-contained executable, copies it to
    %LOCALAPPDATA%\Programs\spoticli-monitor, and registers a Scheduled Task named
    "spoticli Top Monitor" that runs it once a day.

    The task runs interactively as the current user. That matters twice over: the monitor
    reads your Spotify login from %APPDATA%\spoticli\config.json, and Windows only delivers
    toast notifications to an interactive session. A Windows Service would satisfy neither.

    Snapshots are stored in %APPDATA%\spoticli\history.db and logs in
    %APPDATA%\spoticli\logs.

    Reporting is via a Windows toast (a one-line summary) and, once configured, an email
    carrying the full ranked detail. See the Email section printed at the end of a successful
    install for how to turn the email on.

    The script is idempotent. If the task already exists, it offers to reinstall (republish +
    replace binaries), run it now, or uninstall it.

    Elevation is NOT required - everything is scoped to the current user.

.PARAMETER Time
    Time of day to run, as HH:mm. Defaults to 00:00 (midnight).

.EXAMPLE
    .\install-monitor.ps1
    .\install-monitor.ps1 -Time 03:30
#>
[CmdletBinding()]
param(
    [ValidatePattern('^\d{2}:\d{2}$')]
    [string]$Time = '00:00'
)

$ErrorActionPreference = 'Stop'

$TaskName   = 'spoticli Top Monitor'
$TaskPath   = '\spoticli\'
$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\spoticli-monitor'
$RepoRoot   = Split-Path -Parent $PSScriptRoot
$Project    = Join-Path $RepoRoot 'src\monitor\monitor.csproj'
$StagingDir = Join-Path $RepoRoot 'build\publish\monitor'
$ExePath    = Join-Path $InstallDir 'sp0-monitor.exe'
$AumidKey   = 'HKCU:\Software\Classes\AppUserModelId\spoticli.TopMonitor'

function Publish-Monitor {
    Write-Host "Publishing monitor to staging..." -ForegroundColor Cyan
    if (Test-Path $StagingDir) {
        Remove-Item $StagingDir -Recurse -Force
    }
    dotnet publish $Project -c Release -o $StagingDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

function Copy-MonitorFiles {
    Write-Host "Copying files to $InstallDir..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force $InstallDir | Out-Null
    Copy-Item (Join-Path $StagingDir '*') $InstallDir -Recurse -Force
}

function Register-MonitorTask {
    Write-Host "Registering scheduled task '$TaskName' for $Time daily..." -ForegroundColor Cyan

    $action = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $InstallDir
    $trigger = New-ScheduledTaskTrigger -Daily -At $Time

    # Interactive logon so the toast reaches the desktop and %APPDATA% resolves to this user.
    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited

    # StartWhenAvailable makes a missed midnight run (machine asleep or logged off) fire once
    # the user is back, rather than being skipped until tomorrow.
    $settings = New-ScheduledTaskSettingsSet `
        -StartWhenAvailable `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -ExecutionTimeLimit (New-TimeSpan -Minutes 15) `
        -MultipleInstances IgnoreNew `
        -Hidden

    Register-ScheduledTask `
        -TaskName $TaskName `
        -TaskPath $TaskPath `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Description 'Snapshots your top 50 Spotify artists and tracks daily and reports what moved.' `
        -Force | Out-Null

    Write-Host "Task registered." -ForegroundColor Green
}

function Unregister-MonitorTask {
    Write-Host "Removing scheduled task '$TaskName'..." -ForegroundColor Cyan
    Unregister-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath -Confirm:$false
    Write-Host "Task removed." -ForegroundColor Green

    if (Test-Path $AumidKey) {
        Remove-Item $AumidKey -Recurse -Force
        Write-Host "Removed the toast notification registration." -ForegroundColor Green
    }

    if (Test-Path $InstallDir) {
        $answer = Read-Host "Remove installed files at $InstallDir as well? [y/N]"
        if ($answer -match '^[yY]') {
            Remove-Item $InstallDir -Recurse -Force
            Write-Host "Removed $InstallDir." -ForegroundColor Green
        }
    }

    $dataDir = Join-Path $env:APPDATA 'spoticli'
    Write-Host "History and logs were left in $dataDir." -ForegroundColor Yellow
}

function Start-MonitorTask {
    Write-Host "Running the monitor now..." -ForegroundColor Cyan
    Start-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath
    Write-Host "Started. Check the log at $env:APPDATA\spoticli\logs." -ForegroundColor Green
}

function Show-Summary {
    Write-Host ""
    Write-Host "Installed:  $ExePath"          -ForegroundColor Gray
    Write-Host "Schedule:   daily at $Time"     -ForegroundColor Gray
    Write-Host "Config:     $env:APPDATA\spoticli\config.json"  -ForegroundColor Gray
    Write-Host "History:    $env:APPDATA\spoticli\history.db"   -ForegroundColor Gray
    Write-Host "Logs:       $env:APPDATA\spoticli\logs"         -ForegroundColor Gray
    Write-Host ""
    Write-Host "Preview a report without touching the history:" -ForegroundColor Gray
    Write-Host "  & '$ExePath' --dry-run"       -ForegroundColor Gray
    Write-Host ""
}

function Show-EmailSetup {
    Write-Host "--- Email report (optional) ---" -ForegroundColor Cyan
    Write-Host "The toast only carries a one-line summary. For the full ranked detail, fill in the" -ForegroundColor Gray
    Write-Host "Monitor.Email section of config.json:" -ForegroundColor Gray
    Write-Host ""
    Write-Host '  "Email": {'                                     -ForegroundColor DarkGray
    Write-Host '    "Enabled": true,'                             -ForegroundColor DarkGray
    Write-Host '    "Host": "127.0.0.1", "Port": 1025,'           -ForegroundColor DarkGray
    Write-Host '    "SecurityMode": "StartTls",'                  -ForegroundColor DarkGray
    Write-Host '    "AcceptSelfSignedCertificate": true,'         -ForegroundColor DarkGray
    Write-Host '    "From": "you@example.com",'                   -ForegroundColor DarkGray
    Write-Host '    "To": "you@example.com",'                     -ForegroundColor DarkGray
    Write-Host '    "UserName": "you@example.com"'                -ForegroundColor DarkGray
    Write-Host '  }'                                              -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Common setups:" -ForegroundColor Gray
    Write-Host "  Proton Mail Bridge  127.0.0.1:1025, StartTls, AcceptSelfSignedCertificate true" -ForegroundColor Gray
    Write-Host "                      (Bridge must be running; use the Bridge-generated password)" -ForegroundColor Gray
    Write-Host "  Gmail               smtp.gmail.com:587, StartTls, self-signed false" -ForegroundColor Gray
    Write-Host "                      (needs 2FA and an app password, not your account password)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "The password is NOT stored in config.json. Set it yourself, in your own shell:" -ForegroundColor Yellow
    Write-Host '  setx SPOTICLI_SMTP_PASSWORD "your-smtp-password"' -ForegroundColor Gray
    Write-Host ""
    Write-Host "setx persists it for your account, which is what lets the scheduled task see it." -ForegroundColor Gray
    Write-Host "Open a new shell afterwards, then prove the settings work:" -ForegroundColor Gray
    Write-Host "  & '$ExePath' --test-email"    -ForegroundColor Gray
    Write-Host ""
}

# --- Main ---

$existing = Get-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath -ErrorAction SilentlyContinue

if ($null -eq $existing) {
    Write-Host "Task '$TaskName' is not installed. Performing fresh install." -ForegroundColor Cyan

    Write-Host ""
    Write-Host "The monitor reads the Spotify login stored by the CLI. If you have not run" -ForegroundColor Yellow
    Write-Host "'sp0 login' as this user yet, do that first or the first run will fail." -ForegroundColor Yellow
    Write-Host ""

    Publish-Monitor
    Copy-MonitorFiles
    Register-MonitorTask
    Show-Summary
    Show-EmailSetup

    $answer = Read-Host "Run it once now to record the baseline? [Y/n]"
    if ($answer -notmatch '^[nN]') {
        Start-MonitorTask
    }
}
else {
    Write-Host "Task '$TaskName' is already installed (state: $($existing.State))." -ForegroundColor Yellow

    $choices = @(
        [System.Management.Automation.Host.ChoiceDescription]::new('&Reinstall', 'Republish the monitor, replace the binaries and re-register the task.')
        [System.Management.Automation.Host.ChoiceDescription]::new('Run &now',   'Trigger the installed task immediately.')
        [System.Management.Automation.Host.ChoiceDescription]::new('&Uninstall', 'Remove the task and the toast registration.')
        [System.Management.Automation.Host.ChoiceDescription]::new('&Cancel',    'Do nothing and exit.')
    )
    $choice = $Host.UI.PromptForChoice("Task already exists", "What would you like to do?", $choices, 0)

    switch ($choice) {
        0 {
            Publish-Monitor
            Copy-MonitorFiles
            Register-MonitorTask
            Show-Summary
        }
        1 { Start-MonitorTask }
        2 { Unregister-MonitorTask }
        3 { Write-Host "Cancelled." }
    }
}
