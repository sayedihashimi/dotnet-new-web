$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition
$outputPath = Join-Path -Path $scriptDir 'output'
$buildConfig = 'release'
# need to create the folder to easily get the full path


function Create-Report{
    [cmdletbinding()]
    param(
        [string]$consoleProjectfile = (Join-Path -Path $scriptDir -ChildPath "src\TemplatesConsole\TemplatesConsole.csproj" ),
        [string]$previousTemplateReport = (Join-Path $scriptDir "src\TemplatesApi\wwwroot\template-report.json")
    )
    process{
        # 1: publish the console project
        if(-not (test-path -LiteralPath $outputPath)){
            New-Item -Path $outputPath -ItemType Directory
        }
        $outputpathtouse = (Get-Item $outputPath).FullName + '\'
        'outputpathtouse: "{0}"' -f $outputpathtouse | Write-Output
        &dotnet publish $consoleProjectfile -p:Configuration=$buildConfig -p:BaseOutputPath=$outputpathtouse

        $pathToExe = (Join-Path -Path $outputPath -ChildPath $buildConfig -AdditionalChildPath 'net6.0\publish\TemplatesConsole.exe')
        if(-not (test-path $pathToExe)){
            'File not found at "{0}"' -f $pathToExe | Write-Error
        }
        # 2: cd into directory with published .exe file
        Push-Location -Path (Split-Path $pathToExe -Parent)
        # 3: run the tool and pass in the parameters
        'call the tool at "{0}" now' -f $pathToExe | Write-Output
        &$pathToExe report --verbose -st template -st templates -st Boilerplate -st generate -st generates -st create -st creates -st Bellatrix -st Meissa -st Scaffold --packageToInclude ServiceStack.Core.Templates --packageToInclude BlackFox.DotnetNew.FSharpTemplates --packageToInclude libyear --packageToInclude angular-cli.dotnet --packageToInclude Carna.ProjectTemplates --packageToInclude SerialSeb.Templates.ClassLibrary --packageToInclude Pioneer.Console.Boilerplate --packageToInclude Microsoft.AspNetCore.Datasync.Template.CSharp --lastReport $previousTemplateReport

        [string]$pathToLatestPkgsToIgnore = (Join-Path $env:LOCALAPPDATA 'telmplatereport\packages-to-ignore.txt')
        if(test-path $pathToLatestPkgsToIgnore){
            Copy-Item -LiteralPath $pathToLatestPkgsToIgnore -Destination (join-path $scriptDir '.\src\TemplatesConsole\packages-to-ignore.txt')
        }

        Pop-Location
    }
}
<#
report --verbose -st template -st templates --packageToInclude ServiceStack.Core.Templates --packageToInclude BlackFox.DotnetNew.FSharpTemplates --packageToInclude libyear --packageToInclude angular-cli.dotnet --packageToInclude Carna.ProjectTemplates --packageToInclude SerialSeb.Templates.ClassLibrary --packageToInclude Pioneer.Console.Boilerplate --lastReport C:\data\mycode\dotnet-new-web\src\TemplatesApi\wwwroot\template-report.json
#>

function DeployTemplateReport{
    [cmdletbinding()]
    param(
        [string]$publishUsername = $env:publishUsername,
        [string]$publishPassword = $env:publishPassword,
        [string]$deployUrl = 'https://dotnetnew-api.scm.azurewebsites.net/msdeploy.axd?site=dotnetnew-api',
        [string]$sourceRelFilepath = 'output/release/net6.0/publish/template-report.json',
        [string]$destRelFilepath = 'wwwroot/wwwroot/template-report.json',
        [string]$ignoreFileFullpath = (join-path $env:LOCALAPPDATA 'templatereport\packages-to-ignore.txt'),
        [string]$destIgnoreRelFilepath = 'wwwroot/wwwroot/packages-to-ignore.txt'
    )
    process{
        # msdeploy -verb:sync -whatif -source:contentPath='C:\data\mycode\sayed-tools\powershell\dotnet\template-report2.json' -dest:contentPath='wwwroot/template-report.json',ComputerName="https://dotnetnew-api.scm.azurewebsites.net/msdeploy.axd?site=dotnetnew-api",UserName='%pubusername%',Password='%pubpwd%',AuthType='Basic'
        if([string]::IsNullOrWhiteSpace($publishUsername)){
            '$publishUsername empty, cannot publish' | Write-Output
            return;
        }
        if([string]::IsNullOrWhiteSpace($publishPassword)){
            '$publishPassword empty, cannot publish' | Write-Output
            return;
        }

        # check that the source file is on disk
        [string]$sourceFile = join-path $scriptDir $sourceRelFilepath | Get-FullPathNormalized
        if(-not (test-path $sourceFile)){
            throw ('source file not found at [{0}], from relpath: [{1}]' -f $sourceFile, $sourceRelFilepath)
        }

        # msdeploy -verb:sync -whatif -source:contentPath=''{0}'' -dest:contentPath=''{1}'',ComputerName="{2}",UserName=''{3}'',Password=''{4}'',AuthType=''Basic'' -retryAttempts=10 -retryInterval=2000 

        # publish the template-report.json
        [string]$msdeployCmdArgs = ('-verb:sync -source:contentPath=''{0}'' -dest:contentPath=''{1}'',ComputerName="{2}",UserName=''{3}'',Password=''{4}'',AuthType=''Basic'' -retryAttempts=10 -retryInterval=2000 ' -f $sourceFile,$destRelFilepath,$deployUrl,$publishUsername,$publishPassword)
        [string]$logfilepath = "$([System.IO.Path]::GetTempFileName()).log"
        New-Item -Path $logfilepath -ItemType File
        'Starting publish for template-report.json, logfile={0}' -f $logfilepath | Write-Output
        try{
            # wrap the call and grab all output. This is needed to mask any secrets that may appear in the logs
            Invoke-CommandString -command (Get-MSDeploy) -commandArgs $msdeployCmdArgs *> $logfilepath
        }
        catch{
        }
        $logcontent = Get-Content $logfilepath
        if($null -eq $logcontent){
            $logcontent = '';
        }
        $logcontent.Replace($publishUsername,'***USERNAME***').Replace($publishPassword,'***PASSWORD***') | Write-Output

        # publish packages-to-ignore.txt
        # check that the source file is on disk
        [string]$ignoreFilePath = join-path $env:LOCALAPPDATA 'templatereport\packages-to-ignore.txt' | Get-FullPathNormalized
        if(-not (test-path $ignoreFilePath)){
            throw ('source file not found at [{0}], from relpath: [{1}]' -f $ignoreFilePath, $sourceRelFilepath)
        }
        [string]$msdeployCmdArgs2 = ('-verb:sync -source:contentPath=''{0}'' -dest:contentPath=''{1}'',ComputerName="{2}",UserName=''{3}'',Password=''{4}'',AuthType=''Basic'' -retryAttempts=10 -retryInterval=2000 ' -f $ignoreFileFullpath,$destIgnoreRelFilepath,$deployUrl,$publishUsername,$publishPassword)
        [string]$logfilepath2 = "$([System.IO.Path]::GetTempFileName()).log"
        New-Item -Path $logfilepath2 -ItemType File
        'Starting publish for packages-to-ignore.txt, logfile={0}' -f $logfilepath | Write-Output
        try{
            # wrap the call and grab all output. This is needed to mask any secrets that may appear in the logs
            Invoke-CommandString -command (Get-MSDeploy) -commandArgs $msdeployCmdArgs2 *> $logfilepath2
        }
        catch{
        }
        $logcontent2 = Get-Content $logfilepath2
        if($null -eq $logcontent2){
            $logcontent2 = '';
        }
        $logcontent2.Replace($publishUsername,'***USERNAME***').Replace($publishPassword,'***PASSWORD***') | Write-Output
    }
}

function Download-LatestTemplateReport{
    [cmdletbinding()]
    param(
        [string]$publishUsername = $env:publishUsername,
        [string]$publishPassword = $env:publishPassword,
        [string]$deployUrl = 'https://dotnetnew-api.scm.azurewebsites.net/msdeploy.axd?site=dotnetnew-api',
        [string]$sourceRelFilepath = 'output/release/net6.0/publish/template-report.json',
        [string]$destRelFilepath = 'wwwroot/wwwroot/template-report.json',
        [string]$ignoreFileFullpath = (join-path $env:LOCALAPPDATA 'templatereport\packages-to-ignore.txt'),
        [string]$destIgnoreRelFilepath = 'wwwroot/wwwroot/packages-to-ignore.txt'
    )
    process{
        if([string]::IsNullOrEmpty($publishUsername)){
            throw 'username is empty'        
        }
        if([string]::IsNullOrEmpty($publishPassword)){
            throw 'password is empty'
        }

        [string]$sourceFile = join-path $scriptDir $sourceRelFilepath | Get-FullPathNormalized
        [string]$msdeployCmdArgs = ('-verb:sync -dest:contentPath=''{0}'' -source:contentPath=''{1}'',ComputerName="{2}",UserName=''{3}'',Password=''{4}'',AuthType=''Basic'' -retryAttempts=10 -retryInterval=2000 ' -f $sourceFile,$destRelFilepath,$deployUrl,$publishUsername,$publishPassword)
        $logfilepath = "$([System.IO.Path]::GetTempFileName()).log"
        New-Item -Path $logfilepath -ItemType File
        'Starting download for template-report.json, logfile={0}' -f $logfilepath | Write-Output
        try{
            'Downloading latest template-report.json from api site' | Write-Output
            # wrap the call and grab all output. This is needed to mask any secrets that may appear in the logs
            Invoke-CommandString -command (Get-MSDeploy) -commandArgs $msdeployCmdArgs *> $logfilepath
        }
        catch{
        }

        $logcontent = Get-Content $logfilepath
        if($null -eq $logcontent){
            $logcontent = ('log file not found at "{0}"' -f $logfilepath);
        }
        $logcontent.Replace($publishUsername,'***USERNAME***').Replace($publishPassword,'***PASSWORD***') | Write-Output


        [string]$sourceFile2 = $ignoreFileFullpath | Get-FullPathNormalized
        [string]$msdeployCmdArgs2 = ('-verb:sync -dest:contentPath=''{0}'' -source:contentPath=''{1}'',ComputerName="{2}",UserName=''{3}'',Password=''{4}'',AuthType=''Basic'' -retryAttempts=10 -retryInterval=2000 ' -f $sourceFile2,$destIgnoreRelFilepath,$deployUrl,$publishUsername,$publishPassword)
        $logfilepath2 = "$([System.IO.Path]::GetTempFileName()).log"
        New-Item -Path $logfilepath2 -ItemType File
        'Starting download for packages-to-ignore.txt, logfile={0}' -f $logfilepath2 | Write-Output
        try{
            'Downloading latest packages-to-ignore.txt from api site' | Write-Output
            # wrap the call and grab all output. This is needed to mask any secrets that may appear in the logs
            Invoke-CommandString -command (Get-MSDeploy) -commandArgs $msdeployCmdArgs2 *> $logfilepath2
            Copy-Item -LiteralPath $sourceFile2 -Destination (Join-Path $scriptDir 'src\TemplatesConsole\packages-to-ignore.txt')
        }
        catch{
        }

        $logcontent2 = Get-Content $logfilepath2
        if($null -eq $logcontent2){
            $logcontent2 = ('log file not found at "{0}"' -f $logfilepath2);
        }
        $logcontent2.Replace($publishUsername,'***USERNAME***').Replace($publishPassword,'***PASSWORD***') | Write-Output
    }
}

function Get-FullPathNormalized{
    [cmdletbinding()]
    param (
        [Parameter(Position=0,ValueFromPipeline=$true)]
        [string[]] $path
    )
    process {
        foreach($p in $path){
            if(-not ([string]::IsNullOrWhiteSpace($p))){
                $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($path)
            }
        }
    }
}
function Get-MSDeploy{
    [cmdletbinding()]
    param(
        [string]$msdeployPath = 'C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe'
    )
    process{
        if(test-path $msdeployPath) {
            Set-Alias -Name msdeploy -Value $msdeployPath -Scope Global
            return $msdeployPath
        }
        else {
            throw ('msdeploye.exe not found at [{0}]' -f $msdeployPath)
        }
    }
}
function Invoke-CommandString{
    [cmdletbinding()]
    param(
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [string[]]$command,
        
        [Parameter(Position=1)]
        $commandArgs,

        $ignoreErrors,

        [bool]$maskSecrets,

        [switch]$disableCommandQuoting
    )
    process{
        foreach($cmdToExec in $command){
            'Executing command [{0}]' -f $cmdToExec | Write-Verbose
            
            # write it to a .cmd file
            $destPath = "$([System.IO.Path]::GetTempFileName()).cmd"
            if(Test-Path $destPath){Remove-Item $destPath|Out-Null}
            
            try{
                $commandstr = $cmdToExec
                if(-not $disableCommandQuoting -and $commandstr.Contains(' ') -and (-not ($commandstr -match '''.*''|".*"' ))){
                    $commandstr = ('"{0}"' -f $commandstr)
                }

                '{0} {1}' -f $commandstr, ($commandArgs -join ' ') | Set-Content -Path $destPath | Out-Null

                $actualCmd = ('"{0}"' -f $destPath)
                cmd.exe /D /C $actualCmd

                if(-not $ignoreErrors -and ($LASTEXITCODE -ne 0)){
                    $msg = ('The command [{0}] exited with code [{1}]' -f $commandstr, $LASTEXITCODE)
                    throw $msg
                }
            }
            finally{
                if(Test-Path $destPath){Remove-Item $destPath -ErrorAction SilentlyContinue |Out-Null}
            }
        }
    }
}

$prnum = $env:APPVEYOR_PULL_REQUEST_NUMBER

# download latest from api site to ensure we always have the latest
# this will ensure that build times are minimal
if([string]::IsNullOrEmpty($prnum) -and (-not ([string]::IsNullOrEmpty($env:publishUsername)))){
    Download-LatestTemplateReport -publishUsername $env:publishUsername -publishPassword $env:publishPassword
}

Create-Report

if([string]::IsNullOrEmpty($prnum) -and (-not ([string]::IsNullOrEmpty($env:publishUsername)))){
    DeployTemplateReport
}