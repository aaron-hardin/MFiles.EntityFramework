using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using MFiles.EntityFramework.Design;
using MFiles.EntityFramework.PowerShell.Extensions;
using MFiles.EntityFramework.PowerShell.Models;
using MFiles.EntityFramework.PowerShell.Utilities;
using MFiles.VaultJsonTools;
using MFiles.VaultJsonTools.ComModels;
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
			string jsonPath = Path.Combine(Project.GetProjectDir(), "Models", "Vault.json");

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
					WriteWarning("JSON file does not exist. Path: "+jsonPath);
					return;
				}
			}

			Assembly assembly = Assembly.LoadFile(GetProjectExecutable(Project));
			WriteLine(assembly.FullName);
			List<Type> types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).ToList();
			List<xObjectClassAdmin> classes = new List<xObjectClassAdmin>();
			foreach (Type type in types)
			{
				WriteLine(type.ToString());
				MetaStructureClassAttribute attr = type.GetCustomAttribute<MetaStructureClassAttribute>();
				if (attr != null)
				{
					string className = attr.Name;
					if (string.IsNullOrWhiteSpace(className))
					{
						className = type.ToString().Split('.').Last();
					}
					WriteLine("\tName: " + className);

					xObjectClassAdmin metaClass = attr.AttributeToComModel();
					classes.Add(metaClass);
				}
			}

			//ProjectUtilities.WriteElements(Project, Project.CodeModel.CodeElements, WriteLine);

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
					string localJsonDiff = File.ReadAllText(jsonPath);
					VaultJSON vJson = Newtonsoft.Json.JsonConvert.DeserializeObject<VaultJSON>(localJsonDiff);
					List<xObjectClassAdmin> classAdmins =
						Newtonsoft.Json.JsonConvert.DeserializeObject<List<xObjectClassAdmin>>(vJson.Classes);
					List<string> resolveProperties = new List<string> {"ID"};
					List<string> compareProperties = new List<string> {"Name", "Workflow"};
					List<VaultDiff<xObjectClassAdmin>> diffs = VaultDiff<xObjectClassAdmin>.GetDiffs(classAdmins, classes,
						resolveProperties, compareProperties);
					WriteLine("DiffCount: "+diffs.Count);
					WriteWarning("Functionality not completed.");
					break;
				default:
					throw new NotImplementedException("DiffMode not implemented.");
			}
		}

		private static string GetProjectExecutable(Project startupProject) //, Configuration config
		{
			string projectFolder = Path.GetDirectoryName(startupProject.FileName);
			string outputPath = startupProject.GetTargetDir(); //(string)config.Properties.Item("OutputPath").Value;
			string assemblyFileName = (string)startupProject.Properties.Item("AssemblyName").Value + ".dll";
			return Path.Combine(new[] {
                                      projectFolder,
                                      outputPath,
                                      assemblyFileName
                                  });
		}
	}
}
