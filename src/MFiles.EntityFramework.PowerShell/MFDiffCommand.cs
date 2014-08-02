using System;
using System.IO;
using MFiles.EntityFramework.PowerShell.Models;
using MFiles.EntityFramework.PowerShell.Utilities;
using MFiles.TestSuite;
using MFiles.VaultJsonTools;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell
{
	internal class MFDiffCommand : MigrationsDomainCommand
	{
		public MFDiffCommand()
		{ }

		public MFDiffCommand(int mode, string password)
		{
			MFDiffCommand migrationCommand = this;
			Execute(() => migrationCommand.Execute(mode, password));
		}

		[STAThread]
		public void Execute(int mode, string password)
		{
			WriteVerbose("Starting diff with mode: " + mode);
			DiffMode diffMode = (DiffMode) mode;

			VaultConnectionSettings connectionSettings = null;
			string jsonPath = Path.Combine("Models", "Vault.json");

			if (diffMode == DiffMode.ServerAndLocal || diffMode == DiffMode.ServerAndModels)
			{
				try
				{
					connectionSettings = SettingsLoader.LoadConnectionSettings(Project, password);
				}
				catch (Exception e)
				{
					WriteWarning("Settings could not be loaded.\n" + e);
					return;
				}
			}
			if (diffMode == DiffMode.LocalAndModels || diffMode == DiffMode.ServerAndLocal)
			{
				if (!File.Exists(jsonPath))
				{
					WriteWarning("JSON file does not exist.");
					return;
				}
			}

			switch (diffMode)
			{
				case DiffMode.ServerAndLocal:
					if (connectionSettings != null)
					{
						Vault vault = connectionSettings.GetServerVault();
						string json = StructureGenerator.VaultToJson(vault);
						string localJson = File.ReadAllText(jsonPath);
						if (json != localJson)
						{
							WriteLine("Changes...");
						}
						else
						{
							WriteLine("No changes.");
						}
					}
					WriteWarning("Functionality not completed.");
					break;
				case DiffMode.ServerAndModels:
					WriteWarning("Functionality not completed.");
					break;
				case DiffMode.LocalAndModels:
					WriteWarning("Functionality not completed.");
					break;
				default:
					throw new NotImplementedException("DiffMode not implemented.");
			}
		}
	}
}
