<#
.SYNOPSIS
    Rename the "Heum" project token to your chosen name.
.EXAMPLE
    .\rename.ps1 -NewName Acme
#>
param(
    [Parameter(Mandatory)]
    [string]$NewName
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$oldPascal = 'Heum'
$newPascal = $NewName
$oldLower  = 'heum'
$newLower  = $NewName.ToLower()

function Replace-InFile([string]$path) {
    $content = Get-Content $path -Raw -Encoding UTF8
    $updated = $content `
        -replace $oldPascal, $newPascal `
        -replace $oldLower,  $newLower
    if ($updated -ne $content) {
        Set-Content $path $updated -Encoding UTF8 -NoNewline
        Write-Host "  updated $path"
    }
}

$contentExts = '*.cs','*.json','*.csproj','*.slnx','*.sln','*.ts','*.tsx','*.ps1','*.sh','*.yml','*.yaml','*.md','*.props','*.targets','*.txt','Dockerfile','*.editorconfig'

Write-Host "Rewriting file contents..."
foreach ($ext in $contentExts) {
    Get-ChildItem -Path $root -Filter $ext -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\(obj|bin|node_modules|\.git)\\' } |
        ForEach-Object { Replace-InFile $_.FullName }
}

Write-Host "Renaming files and directories..."
Get-ChildItem -Path $root -Recurse |
    Where-Object { $_.FullName -notmatch '\\(obj|bin|node_modules|\.git)\\' } |
    Where-Object { $_.Name -match $oldPascal -or $_.Name -match $oldLower } |
    Sort-Object -Property FullName -Descending |
    ForEach-Object {
        $newName = $_.Name -replace $oldPascal, $newPascal -replace $oldLower, $newLower
        if ($newName -ne $_.Name) {
            $dest = Join-Path $_.Directory.FullName $newName
            Move-Item $_.FullName $dest
            Write-Host "  renamed $($_.Name) -> $newName"
        }
    }

Write-Host "Done. Review the changes with 'git diff --stat'."
