param(
    [string]$CorePath = "Core/GSheetToDataCore",
    [string]$SerializableTypesPath = "Core/SerializableTypes"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

$coreSrc = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $CorePath))
$serializableSrc = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $SerializableTypesPath))
$packageRoot = Join-Path $repoRoot "Packages/com.arandra.gsheet-to-data"
$manifestPath = Join-Path $packageRoot ".sync-manifest.json"
$runtimeDest = (Join-Path $packageRoot "Runtime")
$editorDest = (Join-Path $packageRoot "Editor")

if (-not (Test-Path $coreSrc) -or -not (Test-Path $serializableSrc)) {
    Write-Error "Core source folders not found. Did you update the submodules?"
}

New-Item -ItemType Directory -Force -Path $runtimeDest | Out-Null
New-Item -ItemType Directory -Force -Path $editorDest | Out-Null

function Sync-Folder {
    param(
        [string]$Source,
        [string]$Destination
    )

    Write-Host "Syncing $Source -> $Destination"
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    robocopy $Source $Destination /E /XD .git .vs bin obj /XF *.user *.csproj *.csproj.nuget.* | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed with exit code $LASTEXITCODE"
    }
}

$serializableDest = Join-Path $runtimeDest "SerializableTypes"
$coreDest = Join-Path $editorDest "GSheetToDataCore"

Sync-Folder $serializableSrc $serializableDest
Sync-Folder $coreSrc $coreDest

$legacyPath = Join-Path $runtimeDest "GSheetToDataCore"
if (Test-Path $legacyPath) {
    Write-Host "Removing legacy Runtime/GSheetToDataCore copy"
    Remove-Item $legacyPath -Recurse -Force
}

$currentFiles = @()
$targets = @(
    @{ Rel = "Runtime/SerializableTypes"; Source = $serializableSrc; Dest = $serializableDest },
    @{ Rel = "Editor/GSheetToDataCore"; Source = $coreSrc; Dest = $coreDest }
)

function Get-FileList($path) {
    $result = @()
    if (-not (Test-Path $path)) { return $result }
    Get-ChildItem $path -Recurse -File | Where-Object { $_.Extension -ne ".meta" } | ForEach-Object {
        $relative = ($_.FullName).Substring($packageRoot.Length + 1).Replace("\", "/")
        $result += $relative
    }
    return $result
}

$currentFiles = @()
foreach ($target in $targets) {
    if (-not (Test-Path $target.Source)) { continue }
    $prefix = $target.Rel
    Get-ChildItem $target.Source -Recurse -File | Where-Object { $_.Extension -notin @(".meta", ".csproj") } | ForEach-Object {
        $relativeInner = $_.FullName.Substring($target.Source.Length + 1).Replace("\", "/")
        $currentFiles += "$prefix/$relativeInner"
    }
}
$currentFiles = $currentFiles | Sort-Object -Unique

$oldFiles = @()
if (Test-Path $manifestPath) {
    $json = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($json -is [System.Collections.IEnumerable]) {
        $oldFiles = $json
    } else {
        $oldFiles = $json.files
    }
} else {
    foreach ($target in $targets) {
        $oldFiles += (Get-FileList $target.Dest)
    }
}

$oldFiles = $oldFiles | Sort-Object -Unique
$removed = Compare-Object -ReferenceObject $oldFiles -DifferenceObject $currentFiles -PassThru | Where-Object { $_ -in $oldFiles } | Sort-Object -Unique
foreach ($rel in $removed) {
    $targetPath = Join-Path $packageRoot $rel
    if (Test-Path $targetPath) {
        Remove-Item $targetPath -Force
    }
    $metaPath = "$targetPath.meta"
    if (Test-Path $metaPath) {
        Remove-Item $metaPath -Force
    }
}

$currentFiles | Sort-Object | ConvertTo-Json | Set-Content $manifestPath -Encoding UTF8

function Get-GuidFromPath([string]$relPath) {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($relPath)
    ($md5.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") }) -join ""
}

function Ensure-FolderMeta([string]$folderPath) {
    $rel = $folderPath.Substring($packageRoot.Length + 1).Replace("\", "/")
    $metaPath = "$folderPath.meta"
    if (Test-Path $metaPath) { return }
    $guid = Get-GuidFromPath $rel
    $content = @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    $content | Set-Content $metaPath -Encoding UTF8
}

function Ensure-FileMeta([string]$filePath) {
    $rel = $filePath.Substring($packageRoot.Length + 1).Replace("\", "/")
    $metaPath = "$filePath.meta"
    if (Test-Path $metaPath) { return }
    $guid = Get-GuidFromPath $rel
    $ext = [System.IO.Path]::GetExtension($filePath).ToLowerInvariant()
    switch ($ext) {
        ".asmdef" {
            $content = @"
fileFormatVersion: 2
guid: $guid
AssemblyDefinitionImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
        }
        ".cs" {
            $content = @"
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
        }
        default {
            $content = @"
fileFormatVersion: 2
guid: $guid
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
        }
    }
    $content | Set-Content $metaPath -Encoding UTF8
}

foreach ($target in $targets) {
    if (-not (Test-Path $target.Dest)) { continue }
    Ensure-FolderMeta $target.Dest
    Get-ChildItem $target.Dest -Recurse -Directory | ForEach-Object {
        Ensure-FolderMeta $_.FullName
    }
    Get-ChildItem $target.Dest -Recurse -File | Where-Object { $_.Extension -ne ".meta" } | ForEach-Object {
        Ensure-FileMeta $_.FullName
    }
}

foreach ($rootName in @("Editor", "Runtime")) {
    $rootPath = Join-Path $packageRoot $rootName
    if (Test-Path $rootPath) {
        Ensure-FolderMeta $rootPath
    }
}

Write-Host "Done. SerializableTypes synced to Runtime/, GSheetToDataCore synced to Editor/."
