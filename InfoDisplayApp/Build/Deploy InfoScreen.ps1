$Server      = "INFOSCREEN"
$Source      = "C:\Users\chris\source\repos\InfoDisplayApp\InfoDisplayApp\bin\Release\net10.0-windows\publish"
$Destination = "\\INFOSCREEN\ServerShare\InfoScreen"

# These directories survive deployment.
$PreserveDirectories = @(
    "InfoDisplayApp.exe.WebView2",
    "logs"
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "       InfoScreen Deployment Tool       " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Source:      $Source"
Write-Host "Destination: $Destination"
Write-Host ""

# ------------------------------------------------------------
# 1. Validate source and destination
# ------------------------------------------------------------

Write-Host "[1/6] Checking deployment paths..." -ForegroundColor Yellow

if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
    Write-Host "ERROR: Build directory does not exist:" -ForegroundColor Red
    Write-Host $Source -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $Destination -PathType Container)) {
    Write-Host "ERROR: Cannot access the server deployment directory:" -ForegroundColor Red
    Write-Host $Destination -ForegroundColor Red
    exit 1
}

# Deliberately hard-coded as an additional safeguard against accidentally
# running Remove-Item against the wrong network directory.
if ($Destination -ne "\\INFOSCREEN\ServerShare\InfoScreen") {
    Write-Host "ERROR: Destination failed safety check." -ForegroundColor Red
    Write-Host "Refusing to delete anything." -ForegroundColor Red
    exit 1
}

Write-Host "Deployment paths OK." -ForegroundColor Green
Write-Host ""

# ------------------------------------------------------------
# 2. Kill InfoScreen
# ------------------------------------------------------------

Write-Host "[2/6] Checking for running InfoScreen processes..." -ForegroundColor Yellow

try {
    $StopResult = Invoke-Command -ComputerName $Server -ScriptBlock {

        $Processes = @(
            Get-Process -Name "InfoDisplayApp" -ErrorAction SilentlyContinue
        )

        if ($Processes.Count -eq 0) {
            return "NOT_RUNNING"
        }

        $Processes | Stop-Process -Force

        # Give Windows a moment to tear the process down and release files.
        $Deadline = [DateTime]::UtcNow.AddSeconds(10)

        do {
            Start-Sleep -Milliseconds 250

            $StillRunning = Get-Process `
                -Name "InfoDisplayApp" `
                -ErrorAction SilentlyContinue

        } while ($StillRunning -and [DateTime]::UtcNow -lt $Deadline)

        if ($StillRunning) {
            return "STILL_RUNNING"
        }

        return "TERMINATED"
    }

    switch ($StopResult) {
        "NOT_RUNNING" {
            Write-Host "InfoScreen is not currently running." -ForegroundColor DarkGray
        }

        "TERMINATED" {
            Write-Host "InfoScreen terminated successfully." -ForegroundColor Green
        }

        "STILL_RUNNING" {
            Write-Host "ERROR: InfoScreen refused to terminate." -ForegroundColor Red
            Write-Host "Deployment aborted." -ForegroundColor Red
            exit 1
        }

        default {
            Write-Host "ERROR: Unexpected response from server: $StopResult" -ForegroundColor Red
            exit 1
        }
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: Unable to contact or control InfoScreen." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Deployment aborted." -ForegroundColor Red
    exit 1
}

Write-Host ""

# ------------------------------------------------------------
# 3. Purge previous deployment
# ------------------------------------------------------------

Write-Host "[3/6] Purging previous deployment..." -ForegroundColor Yellow

foreach ($Directory in $PreserveDirectories) {
    Write-Host "      Preserving: $Directory" -ForegroundColor DarkGray
}

try {
    $ItemsToRemove = Get-ChildItem `
        -LiteralPath $Destination `
        -Force |
        Where-Object {
            $_.Name -notin $PreserveDirectories
        }

    $RemovedCount = @($ItemsToRemove).Count

    if ($RemovedCount -gt 0) {
        $ItemsToRemove |
            Remove-Item `
                -Recurse `
                -Force `
                -ErrorAction Stop
    }

    Write-Host "Purged $RemovedCount top-level item(s)." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "ERROR: Unable to purge the old deployment." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Deployment aborted." -ForegroundColor Red
    exit 1
}

Write-Host ""

# ------------------------------------------------------------
# 4. Copy fresh build
# ------------------------------------------------------------

Write-Host "[4/6] Copying fresh InfoScreen build..." -ForegroundColor Yellow
Write-Host ""

$ExcludedSourceDirectories = @(
    "$Source\InfoDisplayApp.exe.WebView2",
    "$Source\logs"
)

& robocopy `
    $Source `
    $Destination `
    /E `
    /MT:16 `
    /R:2 `
    /W:1 `
    /FFT `
    /NP `
    /XD $ExcludedSourceDirectories

$RoboCopyExitCode = $LASTEXITCODE

Write-Host ""

# Robocopy's success codes are weird:
# 0-7 = successful/nonfatal
# 8+  = actual failure
if ($RoboCopyExitCode -ge 8) {
    Write-Host "ERROR: Robocopy failed." -ForegroundColor Red
    Write-Host "Exit code: $RoboCopyExitCode" -ForegroundColor Red
    Write-Host ""
    Write-Host "InfoScreen has NOT been restarted." -ForegroundColor Red
    exit $RoboCopyExitCode
}

Write-Host "File transfer completed successfully." -ForegroundColor Green
Write-Host "Robocopy exit code: $RoboCopyExitCode" -ForegroundColor DarkGray
Write-Host ""

# ------------------------------------------------------------
# 5. Verify deployment
# ------------------------------------------------------------

Write-Host "[5/6] Verifying deployment..." -ForegroundColor Yellow

$ServerExecutable = Join-Path $Destination "InfoDisplayApp.exe"

if (-not (Test-Path -LiteralPath $ServerExecutable -PathType Leaf)) {
    Write-Host ""
    Write-Host "ERROR: InfoDisplayApp.exe was not found after deployment." -ForegroundColor Red
    Write-Host "Expected:" -ForegroundColor Red
    Write-Host $ServerExecutable -ForegroundColor Red
    exit 1
}

Write-Host "InfoDisplayApp.exe found." -ForegroundColor Green

foreach ($Directory in $PreserveDirectories) {

    $PreservedPath = Join-Path $Destination $Directory

    if (Test-Path -LiteralPath $PreservedPath -PathType Container) {
        Write-Host "Preserved: $Directory" -ForegroundColor Green
    }
    else {
        Write-Host "Note: $Directory does not currently exist." -ForegroundColor DarkGray
    }
}

Write-Host ""

# ------------------------------------------------------------
# 6. Resurrect InfoScreen in the interactive desktop session
# ------------------------------------------------------------

Write-Host "[6/6] Resurrecting InfoScreen..." -ForegroundColor Yellow

try {
    $StartResult = Invoke-Command -ComputerName $Server -ScriptBlock {
        $TaskName = "InfoScreen-Deploy-Launch"

        # Resolve the server-local path behind the ServerShare SMB share so the
        # scheduled task launches the executable locally instead of through UNC.
        try {
            $Share = Get-SmbShare -Name "ServerShare" -ErrorAction Stop
        }
        catch {
            return "SHARE_NOT_FOUND"
        }

        $WorkingDir = Join-Path $Share.Path "InfoScreen"
        $Executable = Join-Path $WorkingDir "InfoDisplayApp.exe"

        if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
            return "EXE_NOT_FOUND"
        }

        # Find the user currently logged into the physical/interactive desktop.
        $LoggedInUser = (Get-CimInstance Win32_ComputerSystem).UserName

        if ([string]::IsNullOrWhiteSpace($LoggedInUser)) {
            return "NO_INTERACTIVE_USER"
        }

        # Remove a stale launcher task if a previous deployment was interrupted.
        Unregister-ScheduledTask `
            -TaskName $TaskName `
            -Confirm:$false `
            -ErrorAction SilentlyContinue

        $Action = New-ScheduledTaskAction `
            -Execute $Executable `
            -WorkingDirectory $WorkingDir

        $Principal = New-ScheduledTaskPrincipal `
            -UserId $LoggedInUser `
            -LogonType Interactive `
            -RunLevel Highest

        $Task = New-ScheduledTask `
            -Action $Action `
            -Principal $Principal

        Register-ScheduledTask `
            -TaskName $TaskName `
            -InputObject $Task `
            -Force | Out-Null

        Start-ScheduledTask -TaskName $TaskName

        # Wait up to 15 seconds for InfoScreen to appear in the process table.
        $Deadline = [DateTime]::UtcNow.AddSeconds(15)
        $Started = $false

        do {
            Start-Sleep -Milliseconds 250

            $Process = Get-Process `
                -Name "InfoDisplayApp" `
                -ErrorAction SilentlyContinue

            if ($Process) {
                $Started = $true
                break
            }
        } while ([DateTime]::UtcNow -lt $Deadline)

        # The task has done its job. Removing its definition does not terminate
        # the application it launched.
        Unregister-ScheduledTask `
            -TaskName $TaskName `
            -Confirm:$false `
            -ErrorAction SilentlyContinue

        if ($Started) {
            return "STARTED"
        }

        return "START_FAILED"
    }

    switch ($StartResult) {
        "STARTED" {
            Write-Host "InfoScreen resurrected successfully." -ForegroundColor Green
        }

        "SHARE_NOT_FOUND" {
            Write-Host "ERROR: Could not resolve the ServerShare path on INFOSCREEN." -ForegroundColor Red
            exit 1
        }

        "EXE_NOT_FOUND" {
            Write-Host "ERROR: InfoDisplayApp.exe could not be found on the server." -ForegroundColor Red
            exit 1
        }

        "NO_INTERACTIVE_USER" {
            Write-Host "ERROR: No user is logged into the InfoScreen desktop." -ForegroundColor Red
            Write-Host "The build was deployed, but the GUI could not be started interactively." -ForegroundColor Red
            exit 1
        }

        "START_FAILED" {
            Write-Host "ERROR: InfoScreen did not start within 15 seconds." -ForegroundColor Red
            exit 1
        }

        default {
            Write-Host "ERROR: Unexpected resurrection response: $StartResult" -ForegroundColor Red
            exit 1
        }
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: Unable to resurrect InfoScreen." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "      INFOSCREEN DEPLOYMENT COMPLETE    " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Fresh application files deployed." -ForegroundColor Green
Write-Host "WebView2 data preserved." -ForegroundColor Green
Write-Host "Logs preserved." -ForegroundColor Green
Write-Host ""
Write-Host "InfoScreen is running." -ForegroundColor Green
Write-Host ""