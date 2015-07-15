function Get-MSBuildCmd
{
        process
        {

             $path =  Get-ChildItem "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\" | 
                                   Sort-Object {[double]$_.PSChildName} -Descending | 
                                   Select-Object -First 1 | 
                                   Get-ItemProperty -Name MSBuildToolsPath |
                                   Select -ExpandProperty MSBuildToolsPath
        
            $path = (Join-Path -Path $path -ChildPath 'msbuild.exe')

        return Get-Item $path
    }
} 

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

#VARS 
$CurrentPath         = $PSScriptRoot
$OutoutDirectoryName = "output" 

$PackageDropPath = Join-Path -Path $CurrentPath -ChildPath "HostingActivationApp\pkg\debug\"
$SolutionFilePath    = Join-Path -Path $CurrentPath -ChildPath "HostingActivation.sln"
$AppProjectPath = Join-Path -Path $CurrentPath -ChildPath "HostingActivationApp\HostingActivationApp.sfproj"




#0) Build the solution 
Write-Host "Step 0 build the solution & create SF app package... press enter to start" -ForegroundColor Green
Read-Host

if(Test-path -Path $PackageDropPath)
{
    Remove-Item -Path $PackageDropPath -Recurse -Force
}

New-Item $PackageDropPath -ItemType Directory



   $msbuildCmd = '"{0}" "{1}" /T:HostingActivationSvcs:rebuild;HostingActivationApp:rebuild  /flp:logfile=C:\Solution.build.log' -f `
        (Get-MSBuildCmd), `
        $SolutionFilePath 
        
    

    #Start execution of the build command // rebuild
    $job = Start-Process cmd.exe -ArgumentList('/C "' + $msbuildCmd + '"') -WindowStyle Normal -Wait -PassThru 
       
    if ($job.ExitCode -ne 0)
    {
        throw('MsBuild exited with an error. ExitCode:' + $job.ExitCode)
    }

    

    $msbuildCmd = '"{0}" "{1}" /T:Package  /flp:logfile=C:\Solution.Package.log' -f `
    (Get-MSBuildCmd), `
    $AppProjectPath 

    
        
    #Start execution of the build command // package
    $job = Start-Process cmd.exe -ArgumentList('/C "' + $msbuildCmd + '"') -WindowStyle Normal -Wait -PassThru   
    if ($job.ExitCode -ne 0)
    {
        throw('MsBuild exited with an error. ExitCode:' + $job.ExitCode)
    }
    


 Write-Host "Connecting to local Service Fabric Cluster"
 Connect-ServiceFabricCluster -ErrorAction Stop # if you are using secure clusters you will need to change the params for this cmdlet


# - 0.1) Copy Application Package
Write-Host "Step 0.1 Validate the application Package... press enter to start" -ForegroundColor Green
Read-Host



$clusterManifestText        = Get-ServiceFabricClusterManifest
$imageStoreConnectionString = Get-ImageStoreConnectionString ([xml] $clusterManifestText)



Test-ServiceFabricApplicationPackage -ApplicationPackagePath $PackageDropPath `
                                     -ErrorAction Stop




[xml]$AppMainfestXML = Get-Content (Join-Path -Path $PackageDropPath -ChildPath "ApplicationManifest.xml")
$AppVer = $AppMainfestXML.ApplicationManifest.ApplicationTypeVersion
$AppPathInStore = "incoming\HostingActivationApp$AppVer" 
 


Write-Host "Copy the application package to cluster imagestore @ $AppPathInStore (using: Copy-ServiceFabricApplicationPackage cmdlet)... press enter to start" -ForegroundColor Green
Read-Host

Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $PackageDropPath `
                                     -ImageStoreConnectionString $imageStoreConnectionString  `
                                     -ApplicationPackagePathInImageStore $AppPathInStore `
                                     -ErrorAction Stop


Write-Host "Register App with Service Fabric (using: Register-ServiceFabricApplicationType cmdlet) ... press enter to start" -ForegroundColor Green
Read-Host
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $AppPathInStore `
                                      -Verbose `
                                      -ErrorAction Stop


Write-Host "Create the first app using application type (using: New-ServiceFabricApplication cmdlet)... press enter to start" -ForegroundColor Green
Read-Host
New-ServiceFabricApplication -ApplicationName "fabric:/HostingActivationApp" `
                             -ApplicationTypeName "HostingActivationApp" `
                             -ApplicationTypeVersion "1.0.0.0"




Write-Host "Check Service Fabric Explorer for application creation, ensure ready on all - will take a while - before moving forwrad" -ForegroundColor Gray


Write-Host "Because of my partitioning strategy Service Fabric created host process instances = # cluster node (one per node)... press enter to display" -ForegroundColor Green
Read-Host

Get-process | Where {$_.ProcessName -eq "HostingActivationSvcs" } | Group-Object -Property ProcessName


 
# - 1) Service in existing app
Write-Host "Activate a new service using different partitioning strategy within existing app (using:New-ServiceFabricService cmdlet)... press enter to start" -ForegroundColor Green
Read-Host


# Create Service Within the same app
New-ServiceFabricService -Stateful `
                         -PartitionSchemeUniformInt64 `
                         -LowKey 1 `
                         -HighKey 26 `
                         -PartitionCount 26  `
                         -ApplicationName "fabric:/HostingActivationApp" `
                         -ServiceName "fabric:/HostingActivationApp/Worker3" `
                         -ServiceTypeName "ComputeSvc" `
                         -HasPersistedState `
                         -TargetReplicaSetSize 3 `
                         -MinReplicaSetSize 3

Write-Host "service Activated. if a host capable of hosting this service exists, it will run in it". -ForegroundColor Gray



Write-Host "My ApplicationManifest.xml contain service template i can activate service within my app based on it (using: New-ServiceFabricServiceFromTemplate)... press enter to start" -ForegroundColor Green
Read-Host

New-ServiceFabricServiceFromTemplate -ApplicationName "fabric:/HostingActivationApp" `
                                     -ServiceName "fabric:/HostingActivationApp/TemplateGW" `
                                     -ServiceTypeName "GatewaySvc"

Write-Host "service Activated. if a host capable of hosting this service exists, it will run in it". -ForegroundColor Gray



Write-Host "All these services created within one app instance, will not impact total # of process hosts... press enter to display" -ForegroundColor Green
Read-Host
Get-process | Where {$_.ProcessName -eq "HostingActivationSvcs" } | Group-Object -Property ProcessName | ft




#- 2) new application instance based on existing app type
Write-Host "Create a new Application Intsance based on existing type (using: New-ServiceFabricApplication cmdlet)... press enter to start" -ForegroundColor Green
Read-Host


New-ServiceFabricApplication -ApplicationName "fabric:/HostingActivationApp02" `
                             -ApplicationTypeName "HostingActivationApp" `
                             -ApplicationTypeVersion "1.0.0.0"

Write-Host "Application created, remember because of fabric isolation model (per app instance). Fabric will create new host instnces for the new app" -ForegroundColor Gray
Write-Host "Application instances are created based on the manifest, custom added services - NOT IN default service node in ServiceManifest.xml - will not be created" -ForegroundColor Gray




Write-Host "new application instance will have to different host processes (total = 2 X # of nodes) ... press enter to display" -ForegroundColor Green
Read-Host

Get-process | Where {$_.ProcessName -eq "HostingActivationSvcs" } | Group-Object -Property ProcessName | ft
