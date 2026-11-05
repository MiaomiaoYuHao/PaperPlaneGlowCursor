$ErrorActionPreference = 'Stop'
Get-Process PaperPlaneGlowClick -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'PaperPlaneGlowClick' -ErrorAction SilentlyContinue
Write-Host 'Click-grow effect stopped and removed from startup.'
