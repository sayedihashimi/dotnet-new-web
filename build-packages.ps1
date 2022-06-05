[string]$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition

function DeleteNuGetPackageCacheFolders{
    [cmdletbinding()]
    param(
        [string[]] $packageIds=('sayedha.templates.tasks'),
        [string] $nugetCacheFolder = (Join-Path $env:USERPROFILE '.nuget' 'packages')
    )
    process{
        'DeleteNuGetPackageCacheFolders' | Write-Output
        if(-not (test-path $nugetCacheFolder)){
            throw 'NuGet cache folder not found at "{0}"' -f $nugetCacheFolder
        }

        foreach($pkgId in $packageIds){
            $pathToLookFor = join-path $nugetCacheFolder $pkgId
            if(test-path $pathToLookFor){
                'Deleting nuget cache folder at "{0}"' -f $pathToLookFor | Write-Output
                remove-Item -Path $pathToLookFor -Recurse
            }            
        }

        # $nugetCacheFolder
    }
}

function BuildPackage{
    [cmdletbinding()]
    param(
        [string]$projectPath = (Join-path $scriptDir 'src\sayedha.templates.tasks\sayedha.templates.tasks.csproj'),
        [string]$buildConfig = 'Release'
    )
    process{
        if(-not (test-path $projectPath)){
            throw 'Project not found at "{0}"' -f $projectPath
        }
        # dotnet pack -c Release C:\data\mycode\dotnet-new-web\src\SayedHa.Templates.Tasks\SayedHa.Templates.Tasks.csproj -bl
        dotnet pack -c $buildConfig $projectPath -bl
    }
}

'Deleting any existing nuget caches'
DeleteNuGetPackageCacheFolders

'Building the package'
BuildPackage


