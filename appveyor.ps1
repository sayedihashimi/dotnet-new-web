$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition
# only run create-templates-report.ps1 if the branch is master and it is not a PR
[string]$isAppveyor = $env:APPVEYOR
[string]$prNumber = $env:APPVEYOR_PULL_REQUEST_NUMBER
[string]$branchName = $env:APPVEYOR_REPO_BRANCH

[string]$cacheModulePath = (Join-Path $scriptDir clean-cache-folder.psm1)


if(-not (test-path -Path $cacheModulePath)){
    'cache module not found at {0}' -f $cacheModulePath | Write-Error
}
else{
    'importing cache module from "{0}"' -f $cacheModulePath | Write-Output
    Import-Module -Name $cacheModulePath -Global -DisableNameChecking
}

function Extract-NuGetCacheAv{
    [cmdletbinding()]
    param(
        [string]$pathToCacheFile = (join-path $scriptDir 'nugettemplatecache.zip')
    )
    process{
        if(-not (Test-Path -Path $pathToCacheFile)){
            'nuget cache file not found at "{0}"' -f $pathToCacheFile | Write-Error
        }
        else{
            Extract-CacheFolder
        }
    }
}

Set-Location -Path C:\projects\dotnet-new-web
.\install-tools.ps1

#'Extracting nuget cache to local folder' | Write-Output
#Extract-NuGetCacheAv

templatereport report --verbose -st template --packageToInclude ServiceStack.Core.Templates --packageToInclude BlackFox.DotnetNew.FSharpTemplates --packageToInclude libyear --packageToInclude angular-cli.dotnet --packageToInclude Carna.ProjectTemplates --packageToInclude SerialSeb.Templates.ClassLibrary --packageToInclude Pioneer.Console.Boilerplate --lastReport C:\projects\dotnet-new-web\src\TemplatesApi\wwwroot\template-report.json -rp C:\projects\dotnet-new-web\src\TemplatesApi\wwwroot\template-report-updated.json



return
# old content below

<#
if( [string]::Compare('true',$isAppveyor,$true) -eq 0 -and
    [string]::IsNullOrEmpty($prNumber) -and
    [string]::Compare('2021.04/cibuild01', $branchName, $true) -eq 0 ){
        #'COMMENTED OUT**** Extracting nuget cache to local folder' | Write-Output
        #Extract-NuGetCacheAv

        '**** Creating template report' | Write-Output
        $createTemplatePath = (Join-Path -Path $scriptDir -ChildPath 'create-template-report.ps1')
        &$createTemplatePath

        # temporary
        Get-ChildItem (join-path $env:LOCALAPPDATA 'templatereport') -Recurse | 
            Select-Object -ExpandProperty FullName |
            Out-File 'C:\projects\dotnet-new-web\ls-out.txt' -Force

        '**** directory results for C:\projects\dotnet-new-web\output\' | Out-File -Append -LiteralPath 'C:\projects\dotnet-new-web\ls-out.txt'

        Get-ChildItem 'C:\projects\dotnet-new-web' -Recurse  |
            Select-Object -ExpandProperty FullName -ErrorAction Continue |
            Out-File -Append 'C:\projects\dotnet-new-web\ls-out.txt' -ErrorAction Continue
}
else{
    'false' | Write-Output
}
#>