# Research repo only: helpers to find this project's open Unity editor and bring it to the foreground.
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class ResearchWindow {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    // Windows blocks SetForegroundWindow from background processes unless an input event just happened; tap Alt first.
    public static bool Front(IntPtr h) {
        if (IsIconic(h)) ShowWindow(h, 9);
        keybd_event(0x12, 0, 0, UIntPtr.Zero); keybd_event(0x12, 0, 2, UIntPtr.Zero);
        SetForegroundWindow(h);
        return GetForegroundWindow() == h;
    }
}
"@
function Get-ResearchUnity {
    $projectDir = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
    $id = (Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -like "*-projectpath*" -and $_.CommandLine -like ("*" + $projectDir + "*") -and $_.CommandLine -notlike "*-batchMode*" } | Select-Object -First 1).ProcessId
    if ($id) { Get-Process -Id $id } else { $null }
}
