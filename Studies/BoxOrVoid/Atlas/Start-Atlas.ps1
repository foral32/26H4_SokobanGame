$ErrorActionPreference = 'Stop'
$atlasUrl = 'http://127.0.0.1:4177'
$alreadyRunning = $false
try { $health = Invoke-RestMethod -Uri ($atlasUrl + '/atlas-health.json') -TimeoutSec 2; $alreadyRunning = $health.app -eq 'box-or-void-td-atlas' } catch {}
if (-not $alreadyRunning) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'dist/index.html'))) { throw 'Missing dist/index.html. Run npm run build in the Atlas folder first.' }
    $nodePath = (Get-Command node -ErrorAction Stop).Source
    $atlasScript = Join-Path $PSScriptRoot 'scripts/server.mjs'
    Start-Process -FilePath $nodePath -ArgumentList ('"' + $atlasScript + '"') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $PSScriptRoot 'server.log') -RedirectStandardError (Join-Path $PSScriptRoot 'server-error.log') | Out-Null
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        Start-Sleep -Milliseconds 200
        try { $health = Invoke-RestMethod -Uri ($atlasUrl + '/atlas-health.json') -TimeoutSec 1; if ($health.app -eq 'box-or-void-td-atlas') { $alreadyRunning = $true; break } } catch {}
    }
}
if (-not $alreadyRunning) { throw 'The atlas did not start. Check server-error.log and port 4177.' }
Start-Process $atlasUrl
