[string]$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition

[string[]]$projects = (Join-Path $scriptDir 'src\Templates\Templates.csproj'),(Join-Path $scriptDir 'src\TemplatesConsole\TemplatesConsole.csproj')

foreach($p in $projects){
    'Building and installing tool for project at: "{0}"' -f $p | Write-Output
    dotnet build $p -t:InstallTool
}