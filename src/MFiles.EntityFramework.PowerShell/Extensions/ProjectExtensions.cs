// This file comes from Microsoft's Entity Framework and is covered under their license.
// See LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using testpkg.PowerShell;
using testpkg.PowerShell.Utilities;

namespace MFiles.EntityFramework.PowerShell.Extensions
{
	internal static class ProjectExtensions
	{
		public const int S_OK = 0;
		public const string WebApplicationProjectTypeGuid = "{349C5851-65DF-11DA-9384-00065B846F21}";
		public const string WebSiteProjectTypeGuid = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
		public const string VsProjectItemKindPhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";

		public static string GetTargetName(this Project project)
		{
			return project.GetPropertyValue<string>("AssemblyName");
		}

		public static string GetProjectDir(this Project project)
		{
			return project.GetPropertyValue<string>("FullPath");
		}

		public static string GetTargetDir(this Project project)
		{
			return Path.Combine(project.GetProjectDir(),
				project.IsWebSiteProject() ? "Bin" : project.GetConfigurationPropertyValue<string>("OutputPath"));
		}

		public static string GetLanguage(this Project project)
		{
			switch (project.CodeModel.Language)
			{
				case "{B5E9BD33-6D3E-4B5D-925E-8A43B79820B4}":
					return "vb";
				case "{B5E9BD34-6D3E-4B5D-925E-8A43B79820B4}":
					return "cs";
				default:
					return null;
			}
		}

		public static string GetRootNamespace(this Project project)
		{
			return project.GetPropertyValue<string>("RootNamespace");
		}

		public static string GetModelNamespace(this Project project)
		{
			return project.GetPropertyValue<string>("RootNamespace")+"."+CodeGenerationHelpers.NAMESPACE;
		}

		public static string GetFileName(this Project project, string projectItemName)
		{
			ProjectItem projectItem;
			try
			{
				projectItem = project.ProjectItems.Item(projectItemName);
			}
			catch
			{
				return Path.Combine(project.GetProjectDir(), projectItemName);
			}
			return projectItem.get_FileNames(0);
		}

		public static bool IsWebProject(this Project project)
		{
			return project.GetProjectTypes(10).Any(g =>
			{
				if (!g.EqualsIgnoreCase("{349C5851-65DF-11DA-9384-00065B846F21}"))
					return g.EqualsIgnoreCase("{E24C65DC-7377-472B-9ABA-BC803B73C61A}");
				return true;
			});
		}

		public static bool IsWebSiteProject(this Project project)
		{
			return project.GetProjectTypes(10).Any(g => g.EqualsIgnoreCase("{E24C65DC-7377-472B-9ABA-BC803B73C61A}"));
		}

		public static void EditFile(this Project project, string path)
		{
			string ItemName = Path.Combine(project.GetProjectDir(), path);
			DTE dte = project.DTE;
			if (dte.SourceControl == null || !dte.SourceControl.IsItemUnderSCC(ItemName) ||
			    dte.SourceControl.IsItemCheckedOut(ItemName))
				return;
			dte.SourceControl.CheckOutItem(ItemName);
		}

		public static void AddFile(this Project project, string path, string contents)
		{
			string path1 = Path.Combine(project.GetProjectDir(), path);
			project.EditFile(path);
			Directory.CreateDirectory(Path.GetDirectoryName(path1));
			File.WriteAllText(path1, contents);
			project.AddFile(path);
		}

		public static void AddFile(this Project project, string path)
		{
			string directoryName = Path.GetDirectoryName(path);
			string fileName = Path.GetFileName(path);
			string projectDir = project.GetProjectDir();
			string FilePath = Path.Combine(projectDir, path);
			ProjectItems projectItems = directoryName.Split(new char[1]
			{
				Path.DirectorySeparatorChar
			}).Aggregate(project.ProjectItems, (pi, dir) =>
			{
				projectDir = Path.Combine(projectDir, dir);
				try
				{
					return pi.Item(dir).ProjectItems;
				}
				catch
				{
				}
				return pi.AddFromDirectory(projectDir).ProjectItems;
			});
			try
			{
				projectItems.Item(fileName);
			}
			catch
			{
				projectItems.AddFromFileCopy(FilePath);
			}
		}

		public static bool TryBuild(this Project project)
		{
			DTE dte = project.DTE;
			//string SolutionConfiguration = dte.Solution.SolutionBuild.ActiveConfiguration[];
			string SolutionConfiguration = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
			dte.Solution.SolutionBuild.BuildProject(SolutionConfiguration, project.UniqueName, true);
			return dte.Solution.SolutionBuild.LastBuildInfo == 0;
		}

		public static void OpenFile(this Project project, string path)
		{
			GetDispatcher().OpenFile(Path.Combine(project.GetProjectDir(), path));
		}

		public static void NewSqlFile(this Project project, string contents)
		{
			DomainDispatcher dispatcher = GetDispatcher();
			try
			{
				dispatcher.NewTextFile(contents, "General\\Sql File");
			}
			catch
			{
				string tempFileName = Path.GetTempFileName();
				File.Delete(tempFileName);
				string path3 = Path.ChangeExtension(Path.GetFileName(tempFileName), ".sql");
				var configurationPropertyValue = project.GetConfigurationPropertyValue<string>("IntermediatePath");
				string str = Path.Combine(project.GetProjectDir(), configurationPropertyValue, path3);
				File.WriteAllText(str, contents);
				dispatcher.OpenFile(str);
			}
		}

		private static T GetPropertyValue<T>(this Project project, string propertyName)
		{
			Property property = project.Properties.Item(propertyName);
			if (property == null)
				return default (T);
			//return (T) property[];
			return (T) property.Value;
		}

		private static T GetConfigurationPropertyValue<T>(this Project project, string propertyName)
		{
			Property property = project.ConfigurationManager.ActiveConfiguration.Properties.Item(propertyName);
			if (property == null)
				return default (T);
			//return (T) property[];
			return (T) property.Value;
		}

		public static IEnumerable<string> GetProjectTypes(this Project project, int shellVersion = 10)
		{
			IVsHierarchy ppHierarchy;
			int projectOfUniqueName = ((IVsSolution)
				((IServiceProvider)
					Activator.CreateInstance(
						Type.GetType(
							string.Format(
								"Microsoft.VisualStudio.Shell.ServiceProvider, Microsoft.VisualStudio.Shell, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
								shellVersion)), new object[1]
								{
									(Microsoft.VisualStudio.OLE.Interop.IServiceProvider) project.DTE
								})).GetService(typeof (IVsSolution))).GetProjectOfUniqueName(project.UniqueName, out ppHierarchy);
			if (projectOfUniqueName != 0)
				Marshal.ThrowExceptionForHR(projectOfUniqueName);
			string pbstrProjTypeGuids;
			int projectTypeGuids = ((IVsAggregatableProject) ppHierarchy).GetAggregateProjectTypeGuids(out pbstrProjTypeGuids);
			if (projectTypeGuids != 0)
				Marshal.ThrowExceptionForHR(projectTypeGuids);
			return pbstrProjTypeGuids.Split(new char[1]
			{
				';'
			});
		}

		private static DomainDispatcher GetDispatcher()
		{
			return (DomainDispatcher) AppDomain.CurrentDomain.GetData("efDispatcher");
		}
	}
}