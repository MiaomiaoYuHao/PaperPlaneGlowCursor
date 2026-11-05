$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$source = Join-Path $here 'source\PaperPlaneGlowClick.cs'
$dist = Join-Path $here 'dist'
$output = Join-Path $dist 'PaperPlaneGlowClick.exe'
$icon = Join-Path $here 'PaperPlaneGlow.ico'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $csc)) {
    throw '.NET Framework C# compiler not found. Windows 10/11 is required.'
}
if (-not (Test-Path -LiteralPath $source)) {
    throw 'Source file not found: source\PaperPlaneGlowClick.cs'
}

New-Item -ItemType Directory -Path $dist -Force | Out-Null
& $csc /nologo /target:winexe /platform:anycpu /optimize+ "/win32icon:$icon" "/out:$output" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $source
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE."
}

Get-Item -LiteralPath $output | Select-Object Name, Length, LastWriteTime