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
using MFiles.TestSuite.ComModels;
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
			base.Execute(() => migrationCommand.Execute(name, force, ignoreChanges));
		}

		[STAThread]
		public void Execute(string password, bool force, bool ignoreChanges)
		{
			if (System.Diagnostics.Debugger.IsAttached == false)
				System.Diagnostics.Debugger.Launch();

			VaultConnectionSettings connectionSettings = null;

			try
			{
				string path = GetAssemblyPath(Project);
				AssemblyName assemblyName = AssemblyName.GetAssemblyName(path);
				Assembly assembly = Assembly.Load(assemblyName);
				Configuration config = ConfigurationManager.OpenExeConfiguration(assembly.Location);
				KeyValueConfigurationCollection settings = config.AppSettings.Settings;

				int authType = LoadAuthType(settings);

				connectionSettings = new VaultConnectionSettings(authType, LoadUsername(settings), password, LoadVaultGuid(settings),
					LoadDomain(settings), LoadAddress(settings), LoadProtocol(settings), LoadPort(settings));

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

		private int LoadAuthType(KeyValueConfigurationCollection settings)
		{
			string value = LoadSetting(settings, "AuthType");

			if (value == null)
				return 1;  // default = MFAuthTypeLoggedOnWindowsUser
			
			int result;
			bool success = int.TryParse(value, out result);
			
			return success ? result : 1;  // default = MFAuthTypeLoggedOnWindowsUser
		}

		private string LoadUsername(KeyValueConfigurationCollection settings)
		{
			return LoadSetting(settings, "Username") ?? "";  // default = ""
		}

		private string LoadVaultGuid(KeyValueConfigurationCollection settings)
		{
			// TODO: ensure that the VaultGuid is not null ahead of time.
			string value = LoadSetting(settings, "VaultGuid");
			if(value == null)
				throw new Exception("VaultGuid Setting is required.");
			return value;
		}

		private string LoadDomain(KeyValueConfigurationCollection settings)
		{
			return LoadSetting(settings, "Domain") ?? "";  // default = ""
		}

		private string LoadAddress(KeyValueConfigurationCollection settings)
		{
			return LoadSetting(settings, "Address") ?? "localhost";  // default = "localhost"
		}

		private ProtocolSequence.Protocol LoadProtocol(KeyValueConfigurationCollection settings)
		{
			string value = LoadSetting(settings, "Protocol");
			
			if(value == null)
				return ProtocolSequence.Protocol.TCPIP;  // default

			try
			{
				return value.GetValueFromDescription<ProtocolSequence.Protocol>();
			}
			catch
			{
				return ProtocolSequence.Protocol.TCPIP;  // default
			}
		}

		private int LoadPort(KeyValueConfigurationCollection settings)
		{
			string value = LoadSetting(settings, "Port");

			if (value == null)
				return 2266;  // default = 2266
			
			int result;
			bool success = int.TryParse(value, out result);
			
			return success ? result : 2266;  // default = "2266"
		}

		private string LoadSetting(KeyValueConfigurationCollection settings, string settingName)
		{
			return settings[settingName] != null ? settings[settingName].Value : null;
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