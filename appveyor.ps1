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
    Import-Module -Name $cacheModulePath -Global -DisableNameChecking
}

function Extract-NuGetCache{
    [cmdletbinding()]
    param(
        [string]$pathToCacheFile = (join-path $scriptDir 'nugettemplatecache.zip')
    )
    process{
        if(-not (Test-Path -Path $pathToCacheFile)){
            'nuget cache file not found at "{0}"' -f $pathToCacheFile | Write-Error
        }
        else{
            Extract-NuGetCache -pathToCacheFile $pathToCacheFile
        }
    }
}

if( [string]::Compare('true',$isAppveyor,$true) -eq 0 -and
    [string]::IsNullOrEmpty($prNumber) -and
    [string]::Compare('master', $branchName, $true) -eq 0 ){
        Extract-NuGetCache
        $createTemplatePath = (Join-Path -Path $scriptDir -ChildPath 'create-template-report.ps1')
        &$createTemplatePath
}

