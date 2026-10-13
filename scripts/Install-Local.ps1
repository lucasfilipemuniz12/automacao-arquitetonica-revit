param([string]$AssemblyPath)
$ErrorActionPreference = 'Stop'
$package = Split-Path -Parent $PSScriptRoot
if (-not $AssemblyPath) { $AssemblyPath = Join-Path $package 'dist-v03\Vebram.Revit.Legal.V03.dll' }
$resolved = (Resolve-Path -LiteralPath $AssemblyPath).Path
if (-not (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $resolved) 'profiles\ARQ_Parametros_Compartilhados.txt'))) {
    throw 'Perfil ARQ ausente junto da DLL.'
}
$addinDir = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2026'
$manifest = Join-Path $addinDir 'Vebram.Legal.addin'
if (Test-Path -LiteralPath $manifest) {
    [xml]$old = Get-Content -LiteralPath $manifest -Raw
    if ($old.RevitAddIns.AddIn.AddInId -ne '936f6231-d0c9-47a3-83e6-65813c46c176') {
        throw 'Já existe um manifesto com esse nome que não pertence a este plugin.'
    }
    Copy-Item -LiteralPath $manifest -Destination ($manifest + '.backup-' + (Get-Date -Format 'yyyyMMddHHmmss'))
}
New-Item -ItemType Directory -Path $addinDir -Force | Out-Null
$previousManifest = Join-Path $addinDir 'Vebram.Automacao.addin'
if (Test-Path -LiteralPath $previousManifest) {
    [xml]$previous = Get-Content -LiteralPath $previousManifest -Raw
    if ($previous.RevitAddIns.AddIn.AddInId -ne 'b5a0f755-65cc-4ba5-8277-475046e8c332') { throw 'Manifesto anterior inesperado.' }
    Move-Item -LiteralPath $previousManifest -Destination ($previousManifest + '.disabled-' + (Get-Date -Format 'yyyyMMddHHmmss'))
}
$xml = New-Object System.Xml.XmlDocument
$root = $xml.CreateElement('RevitAddIns'); [void]$xml.AppendChild($root)
$add = $xml.CreateElement('AddIn'); $add.SetAttribute('Type','Application'); [void]$root.AppendChild($add)
$values = @{
    Name = 'VEBRAM Automacao'; Assembly = $resolved;
    AddInId = '936f6231-d0c9-47a3-83e6-65813c46c176'; FullClassName = 'Vebram.Revit.App';
    VendorId = 'VEBR'; VendorDescription = 'Automacao arquitetonica de Lucas'; AllowLoadIntoExistingSession = 'True'
}
foreach ($entry in $values.GetEnumerator()) {
    $node = $xml.CreateElement($entry.Key); $node.InnerText = $entry.Value; [void]$add.AppendChild($node)
}
$xml.Save($manifest)
Write-Output "Manifesto instalado: $manifest"
Write-Output "DLL: $resolved"

