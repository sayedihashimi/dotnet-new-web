[string]$scriptDir = $PSScriptRoot
[string]$rootCacheFolder = (Join-Path -Path $env:LOCALAPPDATA -ChildPath templatereport)
[string]$defaultCacheFolder = (Join-Path -Path $rootCacheFolder -ChildPath extracted)
[string]$defaultZipOutputFolder = (Join-Path $defaultCacheFolder 'generated-zips')

function Resolve-FullPath{
    [cmdletbinding()]
    param
    (
        [Parameter(Position=0,ValueFromPipeline=$true)]
        [string[]] $path
    )
    process{
        foreach($p in $path){
            if(-not ([string]::IsNullOrWhiteSpace($p))){
                $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($path)
            }
        }
    }
}
function Clean-CacheFolder{
    [cmdletbinding()]
    param(
        [string]$cacheFolder = $defaultCacheFolder
    )
    process{
        if(-not (test-path $cacheFolder)){
            'cache folder not found at "{0}"' -f $cacheFolder | Write-Error
        }
        else{
            'Cleaning cache folder at "{0}"' -f $cacheFolder | Write-Output
            Push-Location -Path $cacheFolder
            [string[]]$dirsToClean = Get-ChildItem -Path $cacheFolder -Directory
            $dirsToClean | Clean-PackageDirectory
            # go back to dir the user was on
            Pop-Location
        }
    }
}

function Clean-PackageDirectory{
    [cmdletbinding()]
    param(
        [parameter(ValueFromPipeline = $true)]
        [string[]]$pkgFolderToClean
    )
    process{
        foreach($pkgfolder in $pkgFolderToClean){
            [string]$fullpath = Resolve-FullPath -path $pkgfolder
            if(-not (test-path $fullpath)){
                'package folder not found at "{0}"' -f $cacheFolder | Write-Error
            }
            else{
                'cleaning pkg folder "{0}"' -f $fullpath | Write-Output
                Get-ChildItem -Path $fullpath -Recurse -File -Exclude 'template.json','*.nuspec','ide.host.json'|Select-Object -ExpandProperty FullName|%{Remove-Item -LiteralPath $_}
                # if the folder doesn't have a template.json file somewhere, delete the folder
                [string[]]$templatejsonfound = Get-ChildItem -Path $fullpath -Recurse -File template.json
                if($templatejsonfound.length -lt 1){
                    '  deleting folder because it doesn''t have any template.json file' | Write-Output
                    Remove-Item -Path $fullpath -Recurse -Force
                }
            }            
        }
    }
}

function Zip-CacheFolder{
    [cmdletbinding()]
    param(
        [string]$cacheFolder = $defaultCacheFolder,
        [string]$zipOutputFolderRoot = $defaultZipOutputFolder
    )
    process{        
        [string]$actualZipOutputPath = (Join-Path $zipOutputFolderRoot -ChildPath ([datetime]::Now.ToString('yyyy.MM.hh.ss.mm')))
        $actualZipOutputPath = Resolve-FullPath -path $actualZipOutputPath

        if(-not (test-path -Path $actualZipOutputPath)){
            New-Item -Path $actualZipOutputPath -ItemType Directory #-Force
        }
        $compress = @{
            Path = $cacheFolder
            # CompressionLevel = "Fastest"
            DestinationPath = (join-path $actualZipOutputPath nugettemplatecache.zip)
        }
        Compress-Archive @compress
    }
}

function Extract-CacheFolder{
    [cmdletbinding()]
    param(
        [string]$sourceZipFile = (Join-Path $scriptDir nugettemplatecache.zip),
        [string]$destination = $defaultCacheFolder
    )
    process{
        #if(-not (test-path -Path $destination)){
        #    New-Item -Path $destination -ItemType Directory
        #}
        Expand-Archive -LiteralPath $sourceZipFile -DestinationPath $destination -Force
    }
}

#Clean-CacheFolder -cacheFolder D:\temp\templates-temp\cache-test
#Zip-CacheFolder -cacheFolder D:\temp\templates-temp\cache-test
#Extract-CacheFolder -destination D:\temp\templates-temp