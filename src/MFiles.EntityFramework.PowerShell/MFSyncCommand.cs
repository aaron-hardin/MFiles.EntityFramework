// This file is based loosely on Microsoft's Entity Framework.
// See LICENSE.txt

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using MFiles.EntityFramework.PowerShell.Extensions;
using MFiles.EntityFramework.PowerShell.Models;
using MFiles.EntityFramework.PowerShell.Utilities;
using MFiles.TestSuite;
using MFilesAPI;

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
			Execute(() => migrationCommand.Execute(name, force, ignoreChanges));
		}

		[STAThread]
		public void Execute(string password, bool force, bool ignoreChanges)
		{
			//if (System.Diagnostics.Debugger.IsAttached == false)
			//	System.Diagnostics.Debugger.Launch();

			VaultConnectionSettings connectionSettings;

			try
			{
				connectionSettings = SettingsLoader.LoadConnectionSettings(Project, password);

				string jsonPath = Path.Combine("Models", "Vault.json");
				if (!force && File.Exists(jsonPath))
				{
					WriteWarning("JSON file exists, use -Force to overwrite.");
					return;
				}
				Vault vault = connectionSettings.GetServerVault();
				string json = StructureGenerator.VaultToJson(vault);
				Project.AddFile(jsonPath, json);
			}
			catch (Exception e)
			{
				connectionSettings = null;
				WriteWarning("Settings could not be loaded.\n"+e);
			}

			if (connectionSettings == null)
				return;
			ModelGenerator generator = new ModelGenerator(this, connectionSettings, force);
			generator.Generate();
		}
	}
}