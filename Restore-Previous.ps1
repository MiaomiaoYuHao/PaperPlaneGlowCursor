$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$backupPath = Join-Path $here 'previous-cursor-config.json'
$cursors = 'HKCU:\Control Panel\Cursors'

if (Test-Path -LiteralPath $backupPath) {
    $backup = Get-Content -LiteralPath $backupPath -Raw | ConvertFrom-Json
    $arrow = $backup.Arrow
    $size = [int]$backup.CursorBaseSize
    $default = $backup.Default
} else {
    $arrow = Join-Path $env:WINDIR 'cursors\aero_arrow.cur'
    $size = 32
    $default = 'Windows Default'
}

Get-Process PaperPlaneGlowClick -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'PaperPlaneGlowClick' -ErrorAction SilentlyContinue
Set-ItemProperty -Path $cursors -Name Arrow -Value $arrow
Set-ItemProperty -Path $cursors -Name CursorBaseSize -Value $size -Type DWord
Set-ItemProperty -Path $cursors -Name '(default)' -Value $default

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class CursorRestoreLocal {
 [DllImport("user32.dll", SetLastError=true)] public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
}
"@
if (-not [CursorRestoreLocal]::SystemParametersInfo(0x0057, 0, [IntPtr]::Zero, 0)) {
    throw "SystemParametersInfo failed: $([Runtime.InteropServices.Marshal]::GetLastWin32Error())"
}

Write-Host 'Previous cursor settings restored and click animation removed.'