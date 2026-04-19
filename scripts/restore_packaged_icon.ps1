param(
  [string]$Base64Path = (Join-Path $PSScriptRoot 'SpeakTextApp.ico.b64'),
  [string]$OutputPath = (Join-Path $PSScriptRoot '..\src\SpeakText.App\Assets\SpeakTextApp.ico')
)

$resolvedBase64Path = [System.IO.Path]::GetFullPath($Base64Path)
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Test-Path -LiteralPath $resolvedBase64Path)) {
  throw "Base64 icon source not found: $resolvedBase64Path"
}

$iconBase64 = [System.IO.File]::ReadAllText($resolvedBase64Path).Trim()
$iconBytes = [Convert]::FromBase64String($iconBase64)

if (
  $iconBytes.Length -lt 4 -or
  $iconBytes[0] -ne 0 -or
  $iconBytes[1] -ne 0 -or
  $iconBytes[2] -ne 1 -or
  $iconBytes[3] -ne 0
) {
  throw "Decoded icon is not a valid ICO file."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
  New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

[System.IO.File]::WriteAllBytes($resolvedOutputPath, $iconBytes)
Write-Host "Restored packaged app icon:" $resolvedOutputPath
