param([int]$Width=1280,[int]$Height=720,[string]$Label='Windows',[string]$Build='Windows',[ValidateSet('Campaign','Themes')][string]$Mode='Campaign')
$ErrorActionPreference='Stop'
$gameRoot=Split-Path $PSScriptRoot -Parent
$projectRoot=Split-Path $gameRoot -Parent
if($Build -notmatch '^[A-Za-z0-9_-]+$'){throw 'Build must be a simple directory name.'}
$gameExe=Join-Path $gameRoot "Builds/$Build/DraculasBox.exe"
if(-not (Test-Path -LiteralPath $gameExe)){throw 'Build the Windows game first.'}
if($Label -notmatch '^[A-Za-z0-9_-]+$'){throw 'Label must be a simple directory name.'}
$output=Join-Path $gameRoot "Evidence/$Label"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$validationFlag=if($Mode -eq 'Themes'){'--dracula-theme-checks'}else{'--dracula-validate'}
$playerArgs=@('-batchmode','-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-logFile',(Join-Path $output 'player.log'),$validationFlag,$output)
$quotedArgs=$playerArgs | ForEach-Object {'"'+[string]$_+'"'}
$player=Start-Process -FilePath $gameExe -WorkingDirectory $projectRoot -ArgumentList $quotedArgs -WindowStyle Hidden -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew()
while(-not $player.WaitForExit(10000)){
    if($timer.Elapsed.TotalMinutes -gt 9){$player | Stop-Process;throw "Own validation process timed out: $($player.Id)"}
}
$result=[pscustomobject]@{processId=$player.Id;exitCode=$player.ExitCode;seconds=[Math]::Round($timer.Elapsed.TotalSeconds,2);width=$Width;height=$Height}
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'process.json') -Encoding utf8
$assemblyPaths=@('DraculasBox.Runtime.dll','DraculasBox.Core.dll') | ForEach-Object {Join-Path $gameRoot "Builds/$Build/DraculasBox_Data/Managed/$_"}
Get-FileHash -LiteralPath $assemblyPaths | Select-Object @{Name='file';Expression={Split-Path $_.Path -Leaf}},Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'assemblies.json') -Encoding utf8
if($player.ExitCode -ne 0){throw "Player validation failed: $Label, exit $($player.ExitCode)"}
$report=Get-Content -LiteralPath (Join-Path $output 'report.json') -Raw -Encoding utf8 | ConvertFrom-Json
if($report.status -ne 'passed' -or $report.width -ne $Width -or $report.height -ne $Height){throw 'Player report failed or resolution differs.'}
[pscustomobject]@{label=$Label;status=$report.status;checks=$report.checks;levels=$report.levels.Count;editorRoundtrip=$report.editorRoundtrip;seconds=$result.seconds} | ConvertTo-Json -Compress
