$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition
$outputPath = Join-Path -Path $scriptDir '.output'
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

        $pathToExe = (Join-Path -Path $outputPath -ChildPath $buildConfig -AdditionalChildPath 'netcoreapp3.1\publish\TemplatesConsole.exe')
        if(-not (test-path $pathToExe)){
            'File not found at "{0}"' -f $pathToExe | Write-Error
        }
        # 2: cd into directory with published .exe file
        Push-Location -Path (Split-Path $pathToExe -Parent)
        # 3: run the tool and pass in the parameters
        'call the tool at "{0}" now' -f $pathToExe | Write-Output
        &$pathToExe report --verbose -st template -st templates --packageToInclude ServiceStack.Core.Templates --packageToInclude BlackFox.DotnetNew.FSharpTemplates --packageToInclude libyear --packageToInclude angular-cli.dotnet --packageToInclude Carna.ProjectTemplates --packageToInclude SerialSeb.Templates.ClassLibrary --packageToInclude Pioneer.Console.Boilerplate --lastReport $previousTemplateReport

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
        [string]$sourceRelFilepath = '.output/release/netcoreapp3.1/publish/template-report.json',
        [string]$destRelFilepath = 'wwwroot/wwwroot/template-report.json'
    )
    process{
    # msdeploy -verb:sync -whatif -source:contentPath='C:\data\mycode\sayed-tools\powershell\dotnet\template-report2.json' -dest:contentPath='wwwroot/template-report.json',ComputerName="https://dotnetnew-api.scm.azurewebsites.net/msdeploy.axd?site=dotnetnew-api",UserName='%pubusername%',Password='%pubpwd%',AuthType='Basic'

        if([string]::IsNullOrWhiteSpace($publishUsername)){
            throw '$publishUsername empty, cannot publish'
        }
        if([string]::IsNullOrWhiteSpace($publishPassword)){
            throw '$publishPassword empty, cannot publish'
        }

        # check that the source file is on disk
        [string]$sourceFile = join-path $scriptDir $sourceRelFilepath | Get-FullPathNormalized
        if(-not (test-path $sourceFile)){
            throw ('source file not found at [{0}], from relpath: [{1}]' -f $sourceFile, $sourceRelFilepath)
        }

        # msdeploy -verb:sync -whatif -source:contentPath=''{0}'' -dest:contentPath=''{1}'',ComputerName="{2}",UserName=''{3}'',Password=''{4}'',AuthType=''Basic'' -retryAttempts=10 -retryInterval=2000 

        [string]$msdeployCmdArgs = ('-verb:sync -source:contentPath=''{0}'' -dest:contentPath=''{1}'',ComputerName="{2}",UserName=''{3}'',Password=''{4}'',AuthType=''Basic'' -retryAttempts=10 -retryInterval=2000 ' -f $sourceFile,$destRelFilepath,$deployUrl,$publishUsername,$publishPassword)
        $logfilepath = "$([System.IO.Path]::GetTempFileName()).log"
        New-Item -Path $logfilepath -ItemType File
        'Starting publish, logfile={0}' -f $logfilepath | Write-Output
        try{
            # wrap the call and grab all output. This is needed to mask any secrets that may appear in the logs
            # Invoke-CommandString -command (Get-MSDeploy) -commandArgs $msdeployCmdArgs
            Invoke-CommandString -command (Get-MSDeploy) -commandArgs $msdeployCmdArgs *> $logfilepath
        }
        catch{
        }
        $logcontent = Get-Content $logfilepath
        if($null -eq $logcontent){
            $logcontent = '';
        }
        $logcontent.Replace($publishUsername,'***USERNAME***').Replace($publishPassword,'***PASSWORD***') | Write-Output
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

Create-Report
DeployTemplateReport