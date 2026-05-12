param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [string]$Configuration = "Release",
  [string]$Project = "AutoDuty/AutoDuty.csproj"
)

Write-Host "Building AutoDuty $Configuration"
Write-Host "AssemblyVersion: $Version"

dotnet build --configuration $Configuration $Project -p:AssemblyVersion=$Version

if ($LASTEXITCODE -ne 0) {
  Write-Error "dotnet build failed."
  exit $LASTEXITCODE
}

Write-Host "Build succeeded."
