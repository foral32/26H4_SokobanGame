param([switch]$FullCampaign)
$ErrorActionPreference = 'Stop'
$studyRoot = Split-Path $PSScriptRoot -Parent
$projectRoot = Split-Path (Split-Path $studyRoot -Parent) -Parent
$studyExe = Join-Path $studyRoot 'Builds/Windows/BoxOrVoidStudy.exe'
$studyEvidence = Join-Path $studyRoot 'Evidence/Creator'

function Invoke-StudyCheck([string]$label, [int]$width, [int]$height, [bool]$campaign) {
    $targetOutput = Join-Path $studyEvidence $label
    New-Item -ItemType Directory -Path $targetOutput -Force | Out-Null
    $checkArgs = @('-batchmode', '-screen-fullscreen', '0', '-screen-width', $width, '-screen-height', $height, '-logFile', (Join-Path $targetOutput 'player.log'))
    if ($campaign) {
        $checkArgs += @('--study-validate', '--study-quiet', '--study-output', $targetOutput)
        if (-not $FullCampaign) { $checkArgs += '--study-smoke' }
    } else { $checkArgs += @('-creator-check', '-creator-check-output', $targetOutput) }
    # Quoting is for Windows argv; all arguments are fixed flags, integers, or local paths.
    $quotedArgs = $checkArgs | ForEach-Object { '"' + [string]$_ + '"' }
    $checkProcess = Start-Process -FilePath $studyExe -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -ArgumentList $quotedArgs
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while (-not $checkProcess.WaitForExit(30000)) {
        $statusPath = Join-Path $targetOutput 'report.json'
        if (Test-Path -LiteralPath $statusPath) {
            $state = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
            Write-Output "$label : $($state.status), $($state.levels.Count) levels, $($state.currentLevel)"
        }
        if ($timer.Elapsed.TotalMinutes -gt 8) { Stop-Process -Id $checkProcess.Id; throw "Own validation process timed out: $label" }
    }
    $result = [pscustomobject]@{label=$label; processId=$checkProcess.Id; exitCode=$checkProcess.ExitCode; seconds=[Math]::Round($timer.Elapsed.TotalSeconds,2); width=$width; height=$height}
    $result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $targetOutput 'process.json') -Encoding utf8
    $assemblyPaths = @('BoxOrVoid.Runtime.dll', 'BoxOrVoid.Core.dll') | ForEach-Object { Join-Path $studyRoot "Builds/Windows/BoxOrVoidStudy_Data/Managed/$_" }
    Get-FileHash -LiteralPath $assemblyPaths | Select-Object @{Name='file';Expression={Split-Path $_.Path -Leaf}},Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $targetOutput 'assemblies.json') -Encoding utf8
    $result | ConvertTo-Json -Compress
    if ($checkProcess.ExitCode -ne 0) { throw "Validation failed: $label" }
}

Invoke-StudyCheck 'Windows' 1280 720 $false
Invoke-StudyCheck 'Windows1920' 1920 1080 $false
Invoke-StudyCheck $(if ($FullCampaign) {'Campaign'} else {'CampaignSmoke'}) 1280 720 $true
