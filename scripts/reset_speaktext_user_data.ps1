$root = Join-Path $env:LOCALAPPDATA 'SpeakText'

if (-not (Test-Path -LiteralPath $root)) {
    Write-Output "No local SpeakText data found at: $root"
    exit 0
}

Remove-Item -LiteralPath $root -Recurse -Force
Write-Output "SpeakText local data removed from: $root"
