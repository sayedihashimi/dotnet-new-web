$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition
# only run create-templates-report.ps1 if the branch is master and it is not a PR
[string]$isAppveyor = $env:APPVEYOR
[string]$prNumber = $env:APPVEYOR_PULL_REQUEST_NUMBER
[string]$branchName = $env:APPVEYOR_REPO_BRANCH

if( [string]::Compare('true',$isAppveyor,$true) -eq 0 -and
    [string]::IsNullOrEmpty($prNumber) -and
    [string]::Compare('master', $branchName, $true) -eq 0 ){
        $createTemplatePath = (Join-Path -Path $scriptDir -ChildPath 'create-template-report.ps1')
        &$createTemplatePath
}
