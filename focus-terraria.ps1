Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);' -Name Win32 -Namespace F
$proc = Get-Process -Name Terraria -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $proc) { echo "NO_TERRARIA"; exit 1 }
$h = $proc.MainWindowHandle
echo "HANDLE:$h TITLE:$($proc.MainWindowTitle)"
$r = 0
try { $r = [F.Win32]::SetForegroundWindow($h); echo "FOCUS_OK:$r" } catch { echo "FOCUS_FAIL:$_" }
