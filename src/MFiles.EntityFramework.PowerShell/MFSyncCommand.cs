// This file is based loosely on Microsoft's Entity Framework.
// See LICENSE.txt

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using MFiles.EntityFramework.PowerShell.Utilities;

namespace MFiles.EntityFramework.PowerShell
{
	internal class MFSyncCommand : MigrationsDomainCommand
	{
		public MFSyncCommand()
		{
		}

		public MFSyncCommand(string name, bool force, bool ignoreChanges)
		{
			MFSyncCommand migrationCommand = this;
			base.Execute(() => migrationCommand.Execute(name, force, ignoreChanges));
		}

		[STAThread]
		public void Execute(string name, bool force, bool ignoreChanges)
		{
			if (System.Diagnostics.Debugger.IsAttached == false)
				System.Diagnostics.Debugger.Launch();

			WriteWarning("gonna fail :(");

			try
			{
				string text = ConfigurationManager.AppSettings["MFSetting"];//VaultConnector.GetSettings();
				if (text == null)
				{
					text = VaultConnector.GetSettings();
				}
				if (text == null)
				{
					string path = GetAssemblyPath(Project);
					AssemblyName assemblyName = AssemblyName.GetAssemblyName(path);
					Assembly assembly = Assembly.Load(assemblyName);
					Configuration config = ConfigurationManager.OpenExeConfiguration(assembly.Location);
					try
					{
						text = "Loaded setting: " + config.AppSettings.Settings["MFSetting"].Value;
					}
					catch
					{
						text = null;
					}
					
				}
				if (text == null)
					WriteLine("Setting not found");
				else
					WriteLine("Setting: " + text);
			}
			catch (Exception)
			{
				
				//throw;
			}

			ModelGenerator generator = new ModelGenerator(this, name, force);
			generator.Generate();

			//if (System.Diagnostics.Debugger.IsAttached == false)
			//	System.Diagnostics.Debugger.Launch();


		}

		static string GetAssemblyPath(EnvDTE.Project vsProject)
		{
			string fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
			string outputPath = vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
			string outputDir = Path.Combine(fullPath, outputPath);
			string outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
			string assemblyPath = Path.Combine(outputDir, outputFileName);
			return assemblyPath;
		}
	}
}