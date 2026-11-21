# Research repo only: leave and re-enter the open Unity editor (this project) so it imports and recompiles changed scripts.
param([int]$Seconds = 30)
. (Join-Path $PSScriptRoot "win.ps1")
$unity = Get-ResearchUnity
if (-not $unity) { Write-Output "Unity editor for this project not found"; exit 1 }
$left = [ResearchWindow]::Front([ResearchWindow]::GetShellWindow())
Start-Sleep -Seconds 2
$front = [ResearchWindow]::Front($unity.MainWindowHandle)
Start-Sleep -Seconds $Seconds
Write-Output "left=$left front=$front title=$($unity.MainWindowTitle)"
