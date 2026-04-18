param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [switch]$Restore,

    [switch]$DeployToGrasshopper,

    [switch]$AllFrameworks,

    [string[]]$Frameworks = @('net48')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$availableFrameworks = @('net48', 'net7.0', 'net8.0')
$selectedFrameworks = if ($AllFrameworks) {
    $availableFrameworks
}
else {
    $Frameworks
}

$unknownFrameworks = $selectedFrameworks | Where-Object { $_ -notin $availableFrameworks }
if ($unknownFrameworks.Count -gt 0) {
    throw "Unsupported framework(s): $($unknownFrameworks -join ', '). Allowed values: $($availableFrameworks -join ', ')."
}

$builds = foreach ($framework in $selectedFrameworks) {
    @{
        Project = 'src\GrasshopperComponents\GrasshopperComponents.csproj'
        TargetFramework = $framework
    }
}

Push-Location $root
try {
    foreach ($build in $builds) {
        $projectPath = Join-Path $root $build.Project
        Write-Host "Building $($build.Project) [$($build.TargetFramework)]..." -ForegroundColor Cyan

        $arguments = @(
            'build',
            $projectPath,
            '--configuration',
            $Configuration,
            "-p:TargetFramework=$($build.TargetFramework)",
            "-p:DeployToGrasshopper=$($DeployToGrasshopper.ToString().ToLowerInvariant())",
            '-p:BuildInParallel=false',
            '-p:RestoreIgnoreFailedSources=true',
            '-p:NuGetAudit=false',
            '--disable-build-servers',
            '-m:1',
            '-v',
            'minimal'
        )

        if (-not $Restore) {
            $arguments += '--no-restore'
        }

        & dotnet @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $($build.Project) [$($build.TargetFramework)]."
        }
    }
}
finally {
    Pop-Location
}

Write-Host "Build completed successfully." -ForegroundColor Green
