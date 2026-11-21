# Research repo only: keep the built player window in front until its done.txt appears.
param([string]$Done, [int]$TimeoutSeconds = 330)
. (Join-Path $PSScriptRoot "win.ps1")
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$front = $false
while (-not (Test-Path $Done) -and (Get-Date) -lt $deadline) {
    $p = Get-Process ProjectL_Research -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if ($p) { $front = [ResearchWindow]::Front($p.MainWindowHandle) }
    Start-Sleep -Seconds 3
}
Write-Output "front=$front done=$(Test-Path $Done)"
