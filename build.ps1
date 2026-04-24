param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [switch]$Restore,

    [bool]$DeployToGrasshopper = $false,

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

if ($DeployToGrasshopper) {
    $runningRhino = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match 'Rhino|grasshopper' }
    if ($runningRhino) {
        $processSummary = $runningRhino | ForEach-Object { "$($_.ProcessName)#$($_.Id)" }
        throw "Deploy requested while Rhino/Grasshopper is running: $($processSummary -join ', '). Close Rhino before deploy builds."
    }
}

$builds = foreach ($framework in $selectedFrameworks) {
    @{
        Project = 'src\GrasshopperComponents\GrasshopperComponents.csproj'
        TargetFramework = $framework
    }
}

function Get-CriticalDeployFiles {
    param(
        [string]$Root,
        [string]$TargetFramework
    )

    $sourceDir = Join-Path $Root "artifacts\\bin\\GrasshopperComponents\\$Configuration\\$TargetFramework"
    $deployDir = Join-Path $env:APPDATA "Grasshopper\\Libraries\\INDTools\\$TargetFramework"

    return @(
        @{
            Name = 'INDGrasshopperComponents.gha'
            Source = Join-Path $sourceDir 'INDGrasshopperComponents.gha'
            Destination = Join-Path $deployDir 'INDGrasshopperComponents.gha'
        },
        @{
            Name = 'INDGrasshopperComponents.dll'
            Source = Join-Path $sourceDir 'INDGrasshopperComponents.dll'
            Destination = Join-Path $deployDir 'INDGrasshopperComponents.dll'
        },
        @{
            Name = 'Crowd.dll'
            Source = Join-Path $sourceDir 'Crowd.dll'
            Destination = Join-Path $deployDir 'Crowd.dll'
        }
    )
}

function Assert-DeployMatchesBuild {
    param(
        [string]$Root,
        [string]$TargetFramework
    )

    $criticalFiles = Get-CriticalDeployFiles -Root $Root -TargetFramework $TargetFramework
    foreach ($file in $criticalFiles) {
        if (-not (Test-Path $file.Source)) {
            throw "Built artifact missing: $($file.Source)"
        }

        if (-not (Test-Path $file.Destination)) {
            throw "Deployed artifact missing: $($file.Destination)"
        }

        $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.Source).Hash
        $destinationHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.Destination).Hash
        if ($sourceHash -ne $destinationHash) {
            throw "Deploy verification failed for $($file.Name). Built file and deployed file hashes differ."
        }
    }

    Write-Host "Deploy verification passed for $TargetFramework." -ForegroundColor Green
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

        if ($DeployToGrasshopper) {
            Assert-DeployMatchesBuild -Root $root -TargetFramework $build.TargetFramework
        }
    }
}
finally {
    Pop-Location
}

Write-Host "Build completed successfully." -ForegroundColor Green
