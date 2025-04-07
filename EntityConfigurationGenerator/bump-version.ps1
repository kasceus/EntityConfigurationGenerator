$manifestPath = "source.extension.vsixmanifest"

[xml]$xml = Get-Content $manifestPath
$nsMgr = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$nsMgr.AddNamespace("ns", "http://schemas.microsoft.com/developer/vsx-schema/2011")

$identityNode = $xml.SelectSingleNode("//ns:Identity", $nsMgr)

if ($identityNode -ne $null) {
    $version = $identityNode.Version
    $parts = $version -split '\.'

    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2] + 1

    $newVersion = "$major.$minor.$patch"
    $identityNode.Version = $newVersion

    $xml.Save($manifestPath)

    Write-Host "✅ VSIX version updated to $newVersion"
    exit 0
}
else {
    Write-Error "❌ Could not find <Identity> node in manifest."
    exit 1
}

