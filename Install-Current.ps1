$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$cursorPath = Join-Path $here 'PaperPlaneGlow.cur'
$exePath = Join-Path $here 'PaperPlaneGlowClick.exe'
$cursors = 'HKCU:\Control Panel\Cursors'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$startupFolder = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startupFolder 'PaperPlaneGlowCursor.lnk'
$backupPath = Join-Path $here 'previous-cursor-config.json'

$requiredFiles = @(
    'PaperPlaneGlow.cur',
    'PaperPlaneGlowClick.ani',
    'PaperPlaneGlow.ico',
    'PaperPlaneGlowClick.exe'
)

foreach ($name in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $here $name))) {
        throw "Required file is missing: $name"
    }
}

if (-not (Test-Path -LiteralPath $backupPath)) {
    $current = Get-ItemProperty -Path $cursors
    $backup = [ordered]@{
        Arrow = if ($current.Arrow) { $current.Arrow } else { Join-Path $env:WINDIR 'cursors\aero_arrow.cur' }
        CursorBaseSize = if ($null -ne $current.CursorBaseSize) { [int]$current.CursorBaseSize } else { 32 }
        SchemeSource = if ($null -ne $current.SchemeSource) { [int]$current.SchemeSource } else { 2 }
        Default = if ($current.'(default)') { $current.'(default)' } else { 'Windows Default' }
    }
    $backup | ConvertTo-Json | Set-Content -LiteralPath $backupPath -Encoding UTF8
}

Get-Process PaperPlaneGlowClick -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 200

Set-ItemProperty -Path $cursors -Name Arrow -Value $cursorPath
Set-ItemProperty -Path $cursors -Name CursorBaseSize -Value 48 -Type DWord
Set-ItemProperty -Path $cursors -Name '(default)' -Value 'Paper Plane Blue Glow'
$startupCommand = '"' + $exePath + '"'
Set-ItemProperty -Path $runKey -Name 'PaperPlaneGlowClick' -Value $startupCommand
$registeredCommand = (Get-ItemProperty -Path $runKey -Name 'PaperPlaneGlowClick' -ErrorAction Stop).PaperPlaneGlowClick
if ($registeredCommand -ne $startupCommand) {
    throw 'Failed to register the login startup entry.'
}

$shortcutRegistered = $false
try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $here
    $shortcut.WindowStyle = 7
    $shortcut.Description = 'Paper Plane Glow Cursor'
    $shortcut.IconLocation = $exePath + ',0'
    $shortcut.Save()
    $shortcutRegistered = Test-Path -LiteralPath $shortcutPath
}
catch {
    Write-Warning "Could not create the Startup folder shortcut: $($_.Exception.Message)"
}

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class PaperPlaneInstaller {
 [DllImport("user32.dll", SetLastError=true)] public static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);
}
"@
if (-not [PaperPlaneInstaller]::SystemParametersInfo(0x0057, 0, [IntPtr]::Zero, 0)) {
    throw "SystemParametersInfo failed: $([Runtime.InteropServices.Marshal]::GetLastWin32Error())"
}

Start-Sleep -Milliseconds 250
$helper = Start-Process -FilePath $exePath -WindowStyle Hidden -PassThru
Start-Sleep -Milliseconds 500
if ($helper.HasExited) {
    throw "PaperPlaneGlowClick exited during startup with code $($helper.ExitCode)."
}

$config = [ordered]@{
    capturedAt = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')
    arrowPath = $cursorPath
    cursorBaseSize = 48
    schemeName = 'Paper Plane Blue Glow'
    startupRun = $startupCommand
    startupShortcut = if ($shortcutRegistered) { $shortcutPath } else { $null }
    helperPath = $exePath
    helperSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $exePath).Hash
    cursorSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $cursorPath).Hash
}
$config | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $here 'current-config.json') -Encoding UTF8

Write-Host "PaperPlaneGlowCursor installed from $here."
Write-Host 'Auto-start on login: enabled for the current user.'
Write-Host 'Right-click the tray icon to disable the effect or exit.'