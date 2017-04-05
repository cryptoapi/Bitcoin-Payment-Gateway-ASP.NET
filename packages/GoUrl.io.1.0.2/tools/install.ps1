param($installPath, $toolsPath, $package, $project)


#save the project file first - this commits the changes made by nuget before this     script runs.
$project.Save()

#Load the csproj file into an xml object
$xml = [XML] (gc $project.FullName)

#grab the namespace from the project element so your xpath works.
$nsmgr = New-Object System.Xml.XmlNamespaceManager -ArgumentList $xml.NameTable
$nsmgr.AddNamespace('a',$xml.Project.GetAttribute("xmlns"))

#link the service designer to the service.cs

$node = $xml.Project.SelectSingleNode("//a:Content[@Include='Models\GourlDb.Context.tt']", $nsmgr)
$depUpon = $xml.CreateElement("DependentUpon", $xml.Project.GetAttribute("xmlns"))
$depUpon.InnerXml = "GourlDb.edmx"
$node.AppendChild($depUpon)

$node = $xml.Project.SelectSingleNode("//a:Compile[@Include='Models\crypto_payments.cs']", $nsmgr)
$depUpon = $xml.CreateElement("DependentUpon", $xml.Project.GetAttribute("xmlns"))
$depUpon.InnerXml = "GourlDb.tt"
$node.AppendChild($depUpon)

$node = $xml.Project.SelectSingleNode("//a:Content[@Include='Models\GourlDb.edmx.diagram']", $nsmgr)
$depUpon = $xml.CreateElement("DependentUpon", $xml.Project.GetAttribute("xmlns"))
$depUpon.InnerXml = "GourlDb.edmx"
$node.AppendChild($depUpon)

$node = $xml.Project.SelectSingleNode("//a:Content[@Include='Models\GourlDb.tt']", $nsmgr)
$depUpon = $xml.CreateElement("DependentUpon", $xml.Project.GetAttribute("xmlns"))
$depUpon.InnerXml = "GourlDb.edmx"
$node.AppendChild($depUpon)

#save the changes.
$xml.Save($project.FullName)

