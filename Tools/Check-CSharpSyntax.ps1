$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$projectRoot=Split-Path $PSScriptRoot -Parent
$failed=$false
$files=Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -Filter '*.cs'
foreach($file in $files){
    $tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file.FullName))
    foreach($diagnostic in $tree.GetDiagnostics()){
        if($diagnostic.Severity -eq 'Error'){
            $failed=$true
            Write-Output ($file.Name+': '+$diagnostic.ToString())
        }
    }
}
if($failed){throw 'C# syntax check failed'}
Write-Output ('PASS: '+$files.Count+' C# source files parsed. This does not replace Unity compilation.')
