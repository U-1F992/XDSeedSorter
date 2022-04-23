Remove-Item -Path "publish" -Recurse -Force

dotnet publish .\XDSeedSorter -c Release -o publish
dotnet publish .\XDStatsInspector -c Release -o publish

$version = [string]([XML](Get-Content ".\XDSeedSorter\XDSeedSorter.csproj")).Project.PropertyGroup.Version;
Compress-Archive -Path .\publish\* -DestinationPath ".\publish\XDSeedSorter.$version.zip"
