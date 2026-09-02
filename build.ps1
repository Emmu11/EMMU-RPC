param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildDirectory = Join-Path $projectRoot "build"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw "The Windows C# compiler was not found. Install .NET Framework 4.7.2 developer tools or a current .NET SDK."
}

New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null

$runnerArguments = @(
    "/nologo", "/optimize+", "/target:winexe", "/platform:anycpu",
    "/win32manifest:$projectRoot\app.manifest",
    "/out:$buildDirectory\GeneratedApp.exe",
    "/reference:System.dll", "/reference:System.Core.dll", "/reference:System.Drawing.dll", "/reference:System.Windows.Forms.dll",
    "$projectRoot\Runner\Program.cs", "$projectRoot\Runner\AssemblyInfo.cs"
)
& $compiler $runnerArguments
if ($LASTEXITCODE -ne 0) { throw "Generated app compilation failed." }

$launcherArguments = @(
    "/nologo", "/optimize+", "/target:winexe", "/platform:anycpu",
    "/win32manifest:$projectRoot\app.manifest",
    "/out:$buildDirectory\EMMU RPC.exe",
    "/win32icon:$projectRoot\Assets\EMMU-RPC.ico",
    "/reference:System.dll", "/reference:System.Core.dll", "/reference:System.Drawing.dll", "/reference:System.Windows.Forms.dll",
    "/resource:$buildDirectory\GeneratedApp.exe,EmmuRpc.Resources.GeneratedApp.exe",
    "$projectRoot\Launcher\Program.cs", "$projectRoot\Launcher\VersionResourceWriter.cs", "$projectRoot\Launcher\AssemblyInfo.cs"
)
& $compiler $launcherArguments
if ($LASTEXITCODE -ne 0) { throw "EMMU RPC compilation failed." }

Write-Host "Built: $buildDirectory\EMMU RPC.exe"
