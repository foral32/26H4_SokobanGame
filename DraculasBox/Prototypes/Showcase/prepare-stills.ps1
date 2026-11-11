$ErrorActionPreference = 'Stop'
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$destination = Join-Path $workspacePath 'Assets/DraculasBox/Showcase/Editor/ReferenceStills'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$sources = [ordered]@{
  'menu.png' = 'DraculasBox/Reviews/EditorUsability-20261111/Campaign/title-wine.png'
  'editor-rectangle.png' = 'DraculasBox/Reviews/EditorUsability-20261111/Workshop/editor_rectangle_preview.png'
  'editor-erase.png' = 'DraculasBox/Reviews/EditorUsability-20261111/Workshop/editor_erase_rectangle_preview.png'
  'editor-base.png' = 'DraculasBox/Reviews/EditorUsability-20261111/Workshop/workshop_editor_final.png'
  'draft.png' = 'DraculasBox/Evidence/Playtest/level_draft_generator.png'
  'campaign.png' = 'DraculasBox/Evidence/LibraryReset-20261111/soft_level_peek.png'
  'campaign-drag.png' = 'DraculasBox/Reviews/EditorUsability-20261111/Campaign/drag-branch.png'
  'copy-picker.png' = 'DraculasBox/Evidence/LibraryReset-20261111/copy_picker.png'
  'import-preview.png' = 'DraculasBox/Evidence/Navigation-20261111/import_preview.png'
  'sun.png' = 'DraculasBox/Reviews/SunIntegration-20261111/production-sun.png'
  'sun-before.png' = 'DraculasBox/Reviews/SunIntegration-20261111/Play/sunlight-production.png'
  'sun-after.png' = 'DraculasBox/Reviews/SunIntegration-20261111/Play/sunlight-pushed.png'
  'lamp.png' = 'DraculasBox/Reviews/SunIntegration-20261111/production-night.png'
  'circuit.png' = 'DraculasBox/Evidence/Playtest/chapter3_doorstop.png'
  'mirrors.png' = 'DraculasBox/Evidence/ApprovedMenu/wine_playing.png'
}
foreach ($entry in $sources.GetEnumerator()) {
  Copy-Item -LiteralPath (Join-Path $workspacePath $entry.Value) -Destination (Join-Path $destination $entry.Key)
}
$sources | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'sources.json') -Encoding UTF8
