$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$launcher = Join-Path $projectRoot "build\EMMU RPC.exe"

if (-not (Test-Path -LiteralPath $launcher)) {
    & (Join-Path $projectRoot "build.ps1")
}

$displayName = 'CON.txt: GTA/V? "Night" <Test> | *'
$encodedName = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($displayName))
$tempRoot = Join-Path $env:TEMP "EMMU-RPC"
$before = @(Get-ChildItem -LiteralPath $tempRoot -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)

$launcherProcess = Start-Process -FilePath $launcher -ArgumentList @("--headless-launch64", $encodedName) -PassThru
$null = $launcherProcess.WaitForExit(5000)
Start-Sleep -Seconds 2

$after = @(Get-ChildItem -LiteralPath $tempRoot -Directory -ErrorAction Stop | Select-Object -ExpandProperty FullName)
$newDirectory = @($after | Where-Object { $_ -notin $before } | Select-Object -Last 1)
if (-not $newDirectory) { throw "No temporary app directory was created." }

$generatedExecutable = (Get-ChildItem -LiteralPath $newDirectory -Filter "*.exe" | Select-Object -First 1).FullName
$child = Get-Process | Where-Object {
    try { $_.Path -eq $generatedExecutable } catch { $false }
} | Select-Object -First 1
if (-not $child) { throw "The generated application is not running." }

$child.Refresh()
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($generatedExecutable)
$checks = [ordered]@{
    LauncherExited = $launcherProcess.HasExited
    ChildIndependent = -not $child.HasExited
    WindowTitlePreserved = $child.MainWindowTitle -eq $displayName
    FileDescriptionPreserved = $version.FileDescription -eq $displayName
    FilenameWasSanitized = [IO.Path]::GetFileName($generatedExecutable) -eq "CON App.txt GTA V Night Test.exe"
}

$null = $child.CloseMainWindow()
$null = $child.WaitForExit(5000)
Start-Sleep -Seconds 4
$checks["ChildClosed"] = $child.HasExited
$checks["TemporaryFilesDeleted"] = -not (Test-Path -LiteralPath $newDirectory)

$checks.GetEnumerator() | ForEach-Object {
    [pscustomobject]@{ Check = $_.Key; Passed = $_.Value }
} | Format-Table -AutoSize

if ($checks.Values -contains $false) {
    throw "One or more EMMU RPC tests failed."
}

Write-Host "All EMMU RPC tests passed."
