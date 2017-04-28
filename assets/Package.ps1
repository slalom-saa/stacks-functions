<#
.SYNOPSIS
    Packages the Stacks - Functions packages.
#>
param (
    $Configuration = "DEBUG",
    $IncrementVersion = $true
)

function Increment-Version() {
    $jsonpath = 'project.json'
    $json = Get-Content -Raw -Path $jsonpath | ConvertFrom-Json
    $versionString = $json.version
    $patchInt = [convert]::ToInt32($versionString.Split(".")[2].Split("-")[0], 10)
    [int]$incPatch = $patchInt + 1
    $patchUpdate = $versionString.Split(".")[0] + "." + $versionString.Split(".")[1] + "." + ($incPatch -as [string]) + "-*"
    $json.version = $patchUpdate
    $json = ConvertTo-Json $json -Depth 100


    $json = Format-Json $json    
    $json | Out-File  -FilePath $jsonpath
}


function Format-Json([Parameter(Mandatory, ValueFromPipeline)][String] $json) {
  $indent = 0;
  ($json -Split '\n' |
    % {
      if ($_ -match '[\}\]]') {
        # This line contains  ] or }, decrement the indentation level
        $indent--
      }
      $line = (' ' * $indent * 2) + $_.TrimStart().Replace(':  ', ': ')
      if ($_ -match '[\{\[]') {
        # This line contains [ or {, increment the indentation level
        $indent++
      }
      $line
  }) -Join "`n"
}

function Go ($Path) {
    Push-Location $Path

    Remove-Item .\Bin -Force -Recurse
    if ($IncrementVersion) {
        Increment-Version
    }
    dotnet build
    dotnet pack --no-build --configuration $Configuration
    copy .\bin\$Configuration\*.nupkg c:\nuget\

    Pop-Location
}

Push-Location $PSScriptRoot

Go ..\src\Slalom.Stacks.Functions

Pop-Location



