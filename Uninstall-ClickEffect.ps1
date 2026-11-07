$ErrorActionPreference = 'Stop'
Get-Process PaperPlaneGlowClick -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'PaperPlaneGlowClick' -ErrorAction SilentlyContinue
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'PaperPlaneGlowCursor.lnk'
Remove-Item -LiteralPath $shortcutPath -Force -ErrorAction SilentlyContinue
Write-Host 'Click-grow effect stopped and removed from startup.'
