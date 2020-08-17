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

        $pathToExe = Join-Path -Path $outputPath -ChildPath $buildConfig -AdditionalChildPath 'netcoreapp3.1\publish\TemplatesConsole.exe'
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


Create-Report









