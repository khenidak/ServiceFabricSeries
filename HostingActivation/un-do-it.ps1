#from Vs.NET tooling
function Get-ImageStoreConnectionString
{
    <#
    .SYNOPSIS 
    Returns the value of the image store connection string from the cluster manifest.

    .PARAMETER ApplicationManifestPath
    Path to the application manifest file.
    #>

    [CmdletBinding()]
    Param
    (
        [xml]
        $ClusterManifest
    )

    $managementSection = $ClusterManifest.ClusterManifest.FabricSettings.Section | ? { $_.Name -eq "Management" }
    return $managementSection.ChildNodes | ? { $_.Name -eq "ImageStoreConnectionString" } | Select-Object -Expand Value
}
 

 #Un Do it

 Write-Host "Connecting.."
 Connect-ServiceFabricCluster -ErrorAction Stop # if you are using secure clusters you will need to change the params for this cmdlet

$clusterManifestText        = Get-ServiceFabricClusterManifest
$imageStoreConnectionString = Get-ImageStoreConnectionString ([xml] $clusterManifestText)



 
 # - 1) Remove app instances 
Write-Host "Remove all app instance created based on HostingActivationApp AppType  (using:Remove-ServiceFabricApplication  cmdlet)... press any key to start" -ForegroundColor Green
Read-Host

Get-ServiceFabricApplication | Where {$_.ApplicationTypeName -eq "HostingActivationApp"} | Remove-ServiceFabricApplication -Force



 # - 2) Remove app type & package
 

#do it script creates these package, if you change pathes or versions you will have to remove packages yourself
$AppPath1 =  "incoming\HostingActivationApp1.0.0.0"

Write-Host "unregister app Type and package  (using:Unregister-ServiceFabricApplicationType cmdlet)... press any key to start" -ForegroundColor Green
Read-Host
 
Unregister-ServiceFabricApplicationType -ApplicationTypeName "HostingActivationApp" -ApplicationTypeVersion "1.0.0.0" -force

Write-Host "Remove app Type and package  (using:Remove-ServiceFabricApplication  cmdlet)... press any key to start" -ForegroundColor Green
Read-Host

$AppPath1 =  "incoming\HostingActivationApp1.0.0.0"
Remove-ServiceFabricApplicationPackage -ApplicationPackagePathInImageStore $AppPath1 `
                                       -ImageStoreConnectionString $imageStoreConnectionString


