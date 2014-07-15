copy src\MFiles.EntityFramework.PowerShell\bin\Debug\MFiles.EntityFramework.PowerShell.dll Packaging\tools\MFiles.EntityFramework.PowerShell.dll
copy src\MFiles.EntityFramework.PowerShell.Helper\bin\Debug\MFiles.EntityFramework.PowerShell.Helper.dll Packaging\tools\MFiles.EntityFramework.PowerShell.Helper.dll
copy src\MFiles.EntityFramework\bin\Debug\MFiles.EntityFramework.dll Packaging\lib\net45\MFiles.EntityFramework.dll

.\Packaging\redist\NuGet.exe pack Packaging\MFilesEntityFramework.nuspec
move *.nupkg Release\