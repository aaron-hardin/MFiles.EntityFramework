# Copyright (c) Microsoft Corporation.  All rights reserved.

$InitialDatabase = '0'

function Get-Diff
{
	[CmdletBinding(DefaultParameterSetName = 'ConnectionStringName')]
    param (
        [parameter(Position = 0,
            Mandatory = $true)]
        [int] $DiffMode,
		[parameter(Position = 1,
            Mandatory = $true)]
		[string] $Password,
        [string] $ProjectName,
        [string] $StartUpProjectName,
        [string] $ConfigurationTypeName,
        [parameter(ParameterSetName = 'ConnectionStringName')]
        [string] $ConnectionStringName,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionString,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionProviderName)

    $runner = New-MigrationsRunner $ProjectName $StartUpProjectName $ConfigurationTypeName $ConnectionStringName $ConnectionString $ConnectionProviderName

    try
    {
        Invoke-RunnerCommand $runner MFiles.EntityFramework.PowerShell.MFDiffCommand @( $DiffMode, $Password )
        $error = Get-RunnerError $runner
        
        if ($error)
        {
            Write-Host $error.StackTrace
            throw $error.Message
        }
    }
    finally
    {
        Remove-Runner $runner
    }
}

function Sync-Initial
{
    [CmdletBinding(DefaultParameterSetName = 'ConnectionStringName')]
    param (
        [parameter(Position = 0,
            Mandatory = $true)]
        [string] $Password,
        [switch] $Force,
        [string] $ProjectName,
        [string] $StartUpProjectName,
        [string] $ConfigurationTypeName,
        [parameter(ParameterSetName = 'ConnectionStringName')]
        [string] $ConnectionStringName,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionString,
        [parameter(ParameterSetName = 'ConnectionStringAndProviderName',
            Mandatory = $true)]
        [string] $ConnectionProviderName,
        [switch] $IgnoreChanges)

    $runner = New-MigrationsRunner $ProjectName $StartUpProjectName $ConfigurationTypeName $ConnectionStringName $ConnectionString $ConnectionProviderName

    try
    {
        Invoke-RunnerCommand $runner MFiles.EntityFramework.PowerShell.MFSyncCommand @( $Password, $Force.IsPresent, $IgnoreChanges.IsPresent )
        $error = Get-RunnerError $runner
        
        if ($error)
        {
            Write-Host $error.StackTrace
            throw $error.Message
        }
    }
    finally
    {
        Remove-Runner $runner
    }
}

function New-MigrationsRunner($ProjectName, $StartUpProjectName, $ConfigurationTypeName, $ConnectionStringName, $ConnectionString, $ConnectionProviderName)
{
    $startUpProject = Get-MigrationsStartUpProject $StartUpProjectName $ProjectName
    Build-Project $startUpProject

    $project = Get-MigrationsProject $ProjectName
    Build-Project $project

    $installPath = Get-PackageInstallPathByName $project "MFilesEntityFramework"
    $toolsPath = Join-Path $installPath tools

    $info = New-Object System.AppDomainSetup -Property @{
            ShadowCopyFiles = 'true';
            ApplicationBase = $installPath;
            PrivateBinPath = 'tools'
        }

    $targetFrameworkVersion = (New-Object System.Runtime.Versioning.FrameworkName ($project.Properties.Item('TargetFrameworkMoniker').Value)).Version

    if ($targetFrameworkVersion -lt (New-Object Version @( 4, 5 )))
    {
        $info.PrivateBinPath += ';lib\net40'

        $dteVersion = [System.Text.RegularExpressions.Regex]::Match($DTE.Version, '^(?<version>\d{1,2}(\.\d{1,2})?)( \(.+\))?$').Groups['version'].Value

        if ((New-Object Version $dteVersion) -lt (New-Object Version @( 11, 0 )))
        {
            $info.ConfigurationFile = Join-Path $toolsPath 'Redirect.config'
        }
        else
        {
            $info.ConfigurationFile = Join-Path $toolsPath 'Redirect.VS11.config'
        }
    }
    else
    {
        $info.PrivateBinPath += ';lib\net45'
        $info.ConfigurationFile = [AppDomain]::CurrentDomain.SetupInformation.ConfigurationFile
    }

    $domain = [AppDomain]::CreateDomain('Migrations', $null, $info)
    $domain.SetData('project', $project)
    $domain.SetData('startUpProject', $startUpProject)
    $domain.SetData('configurationTypeName', $ConfigurationTypeName)
    $domain.SetData('connectionStringName', $ConnectionStringName)
    $domain.SetData('connectionString', $ConnectionString)
    $domain.SetData('connectionProviderName', $ConnectionProviderName)
    
    [AppDomain]::CurrentDomain.SetShadowCopyFiles()
	
	Write-Verbose "Loading utility assembly."
	
    $utilityAssembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $toolsPath MFiles.EntityFramework.PowerShell.Helper.dll))
    
	Write-Verbose "Creating dispatcher."
	
	$dispatcher = $utilityAssembly.CreateInstance(
        'MFiles.EntityFramework.PowerShell.Helper.DomainDispatcher',
        $false,
        [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::Public,
        $null,
        $PSCmdlet,
        $null,
        $null)        
		
	Write-Verbose "Setting dispatcher on Domain."
	
    $domain.SetData('efDispatcher', $dispatcher)

    return @{
        Domain = $domain;
        ToolsPath = $toolsPath
    }
}

function Remove-Runner($runner)
{
    [AppDomain]::Unload($runner.Domain)
}

function Invoke-RunnerCommand($runner, $command, $parameters, $anonymousArguments)
{
    $domain = $runner.Domain

	if ($anonymousArguments)
	{
		$anonymousArguments.GetEnumerator() | %{
			$domain.SetData($_.Name, $_.Value)
		}
	}

    $domain.CreateInstanceFrom(
        (Join-Path $runner.ToolsPath MFiles.EntityFramework.PowerShell.dll),
        $command,
        $false,
        0,
        $null,
        $parameters,
        $null,
        $null) | Out-Null
}

function Get-RunnerError($runner)
{
    $domain = $runner.Domain

    if (!$domain.GetData('wasError'))
    {
        return $null
    }

    return @{
            Message = $domain.GetData('error.Message');
            TypeName = $domain.GetData('error.TypeName');
            StackTrace = $domain.GetData('error.StackTrace')
    }
}

function Get-MigrationsProject($name, $hideMessage)
{
    if ($name)
    {
        return Get-SingleProject $name
    }

    $project = Get-Project
    $projectName = $project.Name

    if (!$hideMessage)
    {
        Write-Verbose "Using NuGet project '$projectName'."
    }

    return $project
}

function Get-MigrationsStartUpProject($name, $fallbackName)
{    
    $startUpProject = $null

    if ($name)
    {
        $startUpProject = Get-SingleProject $name
    }
    else
    {
        $startupProjectPaths = $DTE.Solution.SolutionBuild.StartupProjects

        if ($startupProjectPaths)
        {
            if ($startupProjectPaths.Length -eq 1)
            {
                $startupProjectPath = $startupProjectPaths[0]

                if (!(Split-Path -IsAbsolute $startupProjectPath))
                {
                    $solutionPath = Split-Path $DTE.Solution.Properties.Item('Path').Value
                    $startupProjectPath = Join-Path $solutionPath $startupProjectPath -Resolve
                }

                $startupProject = Get-SolutionProjects | ?{
                    try
                    {
                        $fullName = $_.FullName
                    }
                    catch [NotImplementedException]
                    {
                        return false;
                    }

                    if ($fullName -and $fullName.EndsWith('\'))
                    {
                        $fullName = $fullName.Substring(0, $fullName.Length - 1)
                    }

                    return $fullName -eq $startupProjectPath
                }
            }
            else
            {
                Write-Verbose 'More than one start-up project found.'
            }
        }
        else
        {
            Write-Verbose 'No start-up project found.'
        }
    }

    if (!($startUpProject -and (Test-StartUpProject $startUpProject)))
    {
        $startUpProject = Get-MigrationsProject $fallbackName $true
        $startUpProjectName = $startUpProject.Name

        Write-Warning "Cannot determine a valid start-up project. Using project '$startUpProjectName' instead. Your configuration file and working directory may not be set as expected. Use the -StartUpProjectName parameter to set one explicitly. Use the -Verbose switch for more information."
    }
    else
    {
        $startUpProjectName = $startUpProject.Name

        Write-Verbose "Using StartUp project '$startUpProjectName'."
    }

    return $startUpProject
}

function Get-SolutionProjects()
{
    $projects = New-Object System.Collections.Stack
    
    $DTE.Solution.Projects | %{
        $projects.Push($_)
    }
    
    while ($projects.Count -ne 0)
    {
        $project = $projects.Pop();
        
        # NOTE: This line is similar to doing a "yield return" in C#
        $project

        if ($project.ProjectItems)
        {
            $project.ProjectItems | ?{ $_.SubProject } | %{
                $projects.Push($_.SubProject)
            }
        }
    }
}

function Get-SingleProject($name)
{
    $project = Get-Project $name

    if ($project -is [array])
    {
        throw "More than one project '$name' was found. Specify the full name of the one to use."
    }

    return $project
}

function Test-StartUpProject($project)
{    
    if ($project.Kind -eq '{cc5fd16d-436d-48ad-a40c-5a424c6e3e79}')
    {
        $projectName = $project.Name
        Write-Verbose "Cannot use start-up project '$projectName'. The Windows Azure Project type isn't supported."
        
        return $false
    }
    
    return $true
}

function Build-Project($project)
{
    $configuration = $DTE.Solution.SolutionBuild.ActiveConfiguration.Name

    $DTE.Solution.SolutionBuild.BuildProject($configuration, $project.UniqueName, $true)

    if ($DTE.Solution.SolutionBuild.LastBuildInfo)
    {
        $projectName = $project.Name

        throw "The project '$projectName' failed to build."
    }
}

function Get-PackageInstallPathByName($project, $packagename)
{
    $package = Get-Package -ProjectName $project.FullName | ?{ $_.Id -eq $packagename }

    if (!$package)
    {
        $projectName = $project.Name

        throw "The '$packagename' package is not installed on project '$projectName'."
    }
    
    return Get-PackageInstallPath $package
}
    
function Get-PackageInstallPath($package)
{
    $componentModel = Get-VsComponentModel
    $packageInstallerServices = $componentModel.GetService([NuGet.VisualStudio.IVsPackageInstallerServices])

    $vsPackage = $packageInstallerServices.GetInstalledPackages() | ?{ $_.Id -eq $package.Id -and $_.Version -eq $package.Version }
    
    return $vsPackage.InstallPath
}

Export-ModuleMember @( 'Sync-Initial', 'Get-Diff' ) -Variable InitialDatabase
