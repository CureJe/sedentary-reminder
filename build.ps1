$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDirectory = Join-Path $projectRoot 'bin'
$applicationName = ([char]0x8D77).ToString() + ([char]0x8EAB).ToString() + ([char]0x5566).ToString()
$outputExecutable = Join-Path $outputDirectory ($applicationName + '.exe')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The Windows .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$compilerArguments = @(
    '/nologo',
    '/target:winexe',
    '/optimize+',
    '/platform:anycpu',
    "/out:$outputExecutable",
    "/win32icon:$(Join-Path $projectRoot 'assets\app\app.ico')",
    "/win32manifest:$(Join-Path $projectRoot 'app.manifest')",
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    "/resource:$(Join-Path $projectRoot 'assets\characters\cat.png'),StandUpBuddy.Assets.cat.png",
    "/resource:$(Join-Path $projectRoot 'assets\characters\corgi.png'),StandUpBuddy.Assets.corgi.png",
    "/resource:$(Join-Path $projectRoot 'assets\characters\red-panda.png'),StandUpBuddy.Assets.red-panda.png",
    "/resource:$(Join-Path $projectRoot 'assets\characters\mech.png'),StandUpBuddy.Assets.mech.png",
    "/resource:$(Join-Path $projectRoot 'assets\characters\web-ranger.png'),StandUpBuddy.Assets.web-ranger.png",
    "/resource:$(Join-Path $projectRoot 'assets\audio\cat.wav'),StandUpBuddy.Audio.cat.wav",
    "/resource:$(Join-Path $projectRoot 'assets\audio\corgi.wav'),StandUpBuddy.Audio.corgi.wav",
    "/resource:$(Join-Path $projectRoot 'assets\audio\red-panda.wav'),StandUpBuddy.Audio.red-panda.wav",
    "/resource:$(Join-Path $projectRoot 'assets\audio\mech.wav'),StandUpBuddy.Audio.mech.wav",
    "/resource:$(Join-Path $projectRoot 'assets\audio\web-ranger.wav'),StandUpBuddy.Audio.web-ranger.wav",
    (Join-Path $projectRoot 'Program.cs'),
    (Join-Path $projectRoot 'AssemblyInfo.cs'),
    (Join-Path $projectRoot 'AppSettings.cs'),
    (Join-Path $projectRoot 'StartupManager.cs'),
    (Join-Path $projectRoot 'CharacterCatalog.cs'),
    (Join-Path $projectRoot 'MainForm.cs'),
    (Join-Path $projectRoot 'ReminderForm.cs')
)

& $compiler $compilerArguments

if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Host "Build complete: $outputExecutable"
