# Research repo only: bring the open Unity editor (this project) to front, run the auto play once, wait for done.txt.
# Usage (repo root): powershell -File LLMTest/core_port/autoplay/run.ps1 [-Label name]
param([string]$Label = "run", [int]$TimeoutSeconds = 330, [string]$Gc = "auto")
. (Join-Path $PSScriptRoot "win.ps1")
$done = Join-Path $PSScriptRoot "done.txt"
Remove-Item $done -ErrorAction SilentlyContinue
$unity = Get-ResearchUnity
if (-not $unity) { Write-Output "Unity editor for this project not found"; exit 1 }
Set-Content -Path (Join-Path $PSScriptRoot "trigger.txt") -Value $Gc -Encoding ascii
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$front = $false
while (-not (Test-Path $done) -and (Get-Date) -lt $deadline) {
    $front = [ResearchWindow]::Front($unity.MainWindowHandle)
    Start-Sleep -Seconds 5
}
Remove-Item (Join-Path $PSScriptRoot "trigger.txt") -ErrorAction SilentlyContinue
if (-not (Test-Path $done)) { Write-Output "timeout waiting for done.txt (front=$front)"; exit 2 }
Write-Output "done: $Label (front=$front)"
