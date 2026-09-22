$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$harness = Join-Path $PSScriptRoot 'NaturalCycleHarness.exe'
$arguments = @('/nologo', '/target:exe', "/out:$harness", '/reference:System.dll',
    '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll')
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'assets/characters') -Filter '*.png') {
    $arguments += "/resource:$($file.FullName),StandUpBuddy.Assets.$($file.Name)"
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'assets/audio') -Filter '*.wav') {
    $arguments += "/resource:$($file.FullName),StandUpBuddy.Audio.$($file.Name)"
}
foreach ($name in @('MainForm.cs', 'ReminderForm.cs', 'CharacterCatalog.cs', 'StartupManager.cs')) {
    $arguments += Join-Path $root $name
}
$arguments += Join-Path $PSScriptRoot 'NaturalCycleHarness.cs'
& $compiler $arguments
if ($LASTEXITCODE -ne 0) { throw 'Natural-cycle harness compilation failed' }
& $harness
if ($LASTEXITCODE -ne 0) { throw 'Natural-cycle checks failed' }
