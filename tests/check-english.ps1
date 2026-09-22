$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $PSScriptRoot 'output'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& (Join-Path $root 'build.ps1')
$harness = Join-Path $PSScriptRoot 'EnglishUiHarness.exe'
& $compiler /nologo /target:exe "/out:$harness" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'EnglishUiHarness.cs')
if ($LASTEXITCODE -ne 0) { throw 'Harness compilation failed.' }
$legacy = Join-Path $output 'legacy-catalog.txt'
$original = & git -C $root show '1372cfaa9c40e197691b5d9b1234af8aa68d16dc:CharacterCatalog.cs'
if ($LASTEXITCODE -ne 0) { throw 'Original compatibility fixture is unavailable.' }
[IO.File]::WriteAllText($legacy, ($original -join "`n"), [Text.Encoding]::UTF8)
try {
    & $harness (Join-Path $root 'bin\StandUpBuddy.exe') $output $legacy
    if ($LASTEXITCODE -ne 0) { throw 'English UI checks failed.' }
} finally {
    Remove-Item -LiteralPath $legacy -Force
}
