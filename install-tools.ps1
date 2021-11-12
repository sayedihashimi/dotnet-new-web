[string]$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition

function DeleteCacheFolders{
    [cmdletbinding()]
    param(
        [string[]]$toolNames = ("sayedha.template.command","templatesconsole"),
        [string[]]$commandName = ("templates","templatereport")
    )
    process{
        'DeleteCacheFolders' | Write-Output

        [string]$toolsFolderPath = Join-Path $env:HOME .dotnet\tools
        [string]$toolsPathFromEnv = $env:TEMPLATEDOTNETTOOLSPATH
        if(-not ([string]::IsNullOrEmpty($toolsPathFromEnv)) -and
                    (test-path $toolsPathFromEnv)){
            'Overriding tools path from env var, env:TEMPLATEDOTNETTOOLSPATH="{0}"' -f $toolsPathFromEnv | Write-Output
            $toolsFolderPath = $toolsPathFromEnv
        }

        foreach($cn in $commandName){
            'DeleteCacheFolders, cn="{0}", env:home="{1}"' -f $cn, $env:HOME | Write-Output
            $exepath = (Join-Path $toolsFolderPath ("{0}.exe" -f $cn))
            ' exepath: "{0}"' -f $exepath | Write-Output
            if(Test-Path $exepath -PathType Leaf){
                Remove-Item -LiteralPath $exepath -Force
            }                        
        }
        foreach($tn in $toolNames){
            'DeleteCacheFolders, tn="{0}"' -f $tn | Write-Output
            $cacheFolder = Join-Path $toolsFolderPath .store $tn
            ' cacheFolder: "{0}"' -f $cacheFolder | Write-Output
            if(Test-Path $cacheFolder -PathType Container){
                Remove-Item -LiteralPath $cacheFolder -Recurse -Force
            }

            # delete nuget package cache files as well
            if($IsWindows){
                $nugetcachefolder = Join-Path $env:LOCALAPPDATA NuGet\v3-cache
            }
            else{
                $nugetcachefolder = resolve-path ~/.nuget/packages
            }
            
            ' nugetcachefolder: "{0}"' -f $nugetcachefolder | Write-Output
            [string[]]$foundnugetfiles = Get-ChildItem -Path $nugetcachefolder ("*{0}*" -f $tn) -Recurse -File
            if($foundnugetfiles -and $foundnugetfiles.Length -gt 0){
                ' foundnugetfiles.Length: "{0}"' -f ($foundnugetfiles.Length) | Write-Output
                Remove-Item -LiteralPath $foundnugetfiles -Force
            }

            # remove shim for non-windows
            if(-not ($IsWindows)){
                foreach($cn in $commandName){
                    $shimpath = (Join-path (resolve-path ~/.dotnet/tools) $cn)

                    if(test-path ($shimpath)){
                        'Removing shim file at "{0}"' -f $shimpath | Write-Output
                        remove-Item -path $shimpath
                    }
                }
            }
        }
    }
}

try {
    DeleteCacheFolders
}
catch {
    Write-Host "An error occurred:"
    Write-Host $_
}
 
# TODO: Both commands not working on linux, fix later
[string[]]$projects = (Join-Path $scriptDir 'src\Templates\Templates.csproj'),(Join-Path $scriptDir 'src\TemplatesConsole\TemplatesConsole.csproj')
# [string[]]$projects = (Join-Path $scriptDir 'src\Templates\Templates.csproj')
foreach($p in $projects){
    'Building and installing tool for project at: "{0}"' -f $p | Write-Output
    dotnet build $p -t:InstallTool -p:Configuration=Release
}