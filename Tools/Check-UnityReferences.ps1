param([string]$EditorData='E:\Unity\Editors\6000.0.62f1\Editor\Data')
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$projectRoot=Split-Path $PSScriptRoot -Parent
$trees=[Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
$parse=[Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithPreprocessorSymbols([string[]]@('UNITY_EDITOR','UNITY_STANDALONE_WIN'))
foreach($file in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -Filter '*.cs'){
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file.FullName),$parse,$file.FullName))
}
$references=[Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$assemblies=@(Get-ChildItem -LiteralPath (Join-Path $EditorData 'NetStandard\ref\2.1.0') -Filter '*.dll')
$assemblies+=@(Get-ChildItem -LiteralPath (Join-Path $EditorData 'Managed\UnityEngine') -Filter '*.dll')
foreach($assembly in $assemblies){$references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($assembly.FullName))}
$options=[Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$compilation=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('FoodArcadeReferenceCheck',$trees,$references,$options)
$errors=@($compilation.GetDiagnostics() | Where-Object {$_.Severity -eq 'Error'})
foreach($item in $errors){Write-Output $item.ToString()}
if($errors.Count -gt 0){throw "$($errors.Count) Unity reference compilation errors"}
Write-Output "PASS: $($trees.Count) C# files compile against installed Unity 6 and .NET Standard 2.1 reference assemblies."
