#!/usr/bin/env pwsh
<#!
.SYNOPSIS
Validates content source files and compiled experience JSON with JSON Schema.

.DESCRIPTION
Runs schema validation for source YAML content and compiled experience JSON using the same
file-based .NET validator app used by local development and CI.

.EXAMPLE
pwsh ./scripts/validate-schema.ps1

.EXAMPLE
pwsh ./scripts/validate-schema.ps1 -SkipReleaseBuild
#>

[CmdletBinding()]
param(
    [string]$DotnetCommand = "dotnet",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$ValidatorPath = (Join-Path $PSScriptRoot "JsonSchemaValidator.cs"),
    [string]$SchemaRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "schemas"),
    [string]$ContentRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "content"),
    [string]$CompiledRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "src/CVApp/wwwroot/data"),
    [string]$BuildProject = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "src/CVApp/CVApp.csproj"),
    [switch]$SkipReleaseBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-SchemaValidation {
    param(
        [Parameter(Mandatory)] [string]$Schema,
        [Parameter(Mandatory)] [string]$BaseDir,
        [Parameter(Mandatory)] [string[]]$Globs,
        [Parameter(Mandatory)] [string]$Label
    )

    Write-Host "[$Label] Schema: $Schema"
    Write-Host "[$Label] BaseDir: $BaseDir"

    $arguments = @('run', $ValidatorPath, '--', '--schema', $Schema, '--base-dir', $BaseDir)
    foreach ($glob in $Globs) {
        $arguments += @('--glob', $glob)
    }

    & $DotnetCommand @arguments | Out-Host
    return ($LASTEXITCODE -eq 0)
}

if (-not (Test-Path -LiteralPath $ValidatorPath)) {
    throw "Validator app not found: $ValidatorPath"
}

if (-not $SkipReleaseBuild) {
    Write-Host "[compiled] Building Release artifact before compiled schema validation..."
    & $DotnetCommand build $BuildProject -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed; cannot validate compiled experience JSON."
    }
}

$sourceChecks = @(
    @{ Label = 'source/employer'; Schema = (Join-Path $SchemaRoot 'content/employer.schema.json'); BaseDir = $ContentRoot; Globs = @('employment/**/employer.yaml') },
    @{ Label = 'source/role'; Schema = (Join-Path $SchemaRoot 'content/role.schema.json'); BaseDir = $ContentRoot; Globs = @('employment/**/role.yaml') },
    @{ Label = 'source/project'; Schema = (Join-Path $SchemaRoot 'content/project.schema.json'); BaseDir = $ContentRoot; Globs = @('employment/**/project.yaml') }
)

$compiledChecks = @(
    @{ Label = 'compiled/experience'; Schema = (Join-Path $SchemaRoot 'compiled/experience.schema.json'); BaseDir = $CompiledRoot; Globs = @('experience.json') }
)

$failed = $false

Write-Host 'Running source content schema validation...'
foreach ($check in $sourceChecks) {
    if (-not (Invoke-SchemaValidation -Schema $check.Schema -BaseDir $check.BaseDir -Globs $check.Globs -Label $check.Label)) {
        $failed = $true
    }
}

Write-Host 'Running compiled experience schema validation...'
foreach ($check in $compiledChecks) {
    if (-not (Invoke-SchemaValidation -Schema $check.Schema -BaseDir $check.BaseDir -Globs $check.Globs -Label $check.Label)) {
        $failed = $true
    }
}

if ($failed) {
    Write-Error 'Schema validation failed.'
    exit 1
}

Write-Host 'All schema validation checks passed.'
