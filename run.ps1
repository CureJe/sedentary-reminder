$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$applicationName = ([char]0x8D77).ToString() + ([char]0x8EAB).ToString() + ([char]0x5566).ToString()
$application = Join-Path $projectRoot ('bin\' + $applicationName + '.exe')

if (-not (Test-Path -LiteralPath $application)) {
    & (Join-Path $projectRoot 'build.ps1')
}

Start-Process -FilePath $application
