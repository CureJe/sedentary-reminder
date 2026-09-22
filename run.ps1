$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$applicationName = 'StandUpBuddy'
$application = Join-Path $projectRoot ('bin\' + $applicationName + '.exe')

if (-not (Test-Path -LiteralPath $application)) {
    & (Join-Path $projectRoot 'build.ps1')
}

Start-Process -FilePath $application
