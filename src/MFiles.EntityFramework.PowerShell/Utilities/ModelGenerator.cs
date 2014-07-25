using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Extensions;
using MFiles.EntityFramework.PowerShell.Models;
using MFiles.EntityFramework.PowerShell.Templates;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	internal class ModelGenerator
	{
		private readonly Project _project;
		private readonly bool _force;
		private readonly MigrationsDomainCommand _command;
		private readonly VaultConnectionSettings _settings;
		private Vault _vault;

		public ModelGenerator(MigrationsDomainCommand command, VaultConnectionSettings settings, bool force)
		{
			_command = command;
			_project = command.Project;
			_settings = settings;
			_force = force;

			if (!IsValid())
			{
				_command.WriteWarning("Configuration invalid.");
				throw new Exception("");
			}
		}

		private Vault Vault
		{
			get { return _vault ?? (_vault = _settings.GetServerVault()); }
		}

		public bool IsValid()
		{
			if (_force)
				return true;

			PropertyDefGenerator pdefGenerator = new PropertyDefGenerator(_project, _vault);
			if (pdefGenerator.Exists)
			{
				_command.WriteWarning(string.Format("File {0} already exists, use -Force to overwrite.", pdefGenerator.FilePath));
				return false;
			}

			ObjVerExGenerator baseGenerator = new ObjVerExGenerator(_project);

			if (baseGenerator.Exists())
			{
				_command.WriteWarning(string.Format("File {0} already exists, use -Force to overwrite.", ObjVerExGenerator.FilePath));
				return false;
			}

			ObjTypes objTypes = Vault.ObjectTypeOperations.GetObjectTypes();

			foreach (ObjType objType in objTypes)
			{
				ObjTypeGenerator otGenerator = new ObjTypeGenerator(objType, _project);
				if (otGenerator.Exists)
				{
					_command.WriteWarning(string.Format("File {0} already exists, use -Force to overwrite.", otGenerator.FilePath));
					return false;
				}

				ObjectClasses objectClasses = Vault.ClassOperations.GetObjectClasses(objType.ID);

				foreach (ObjectClass objectClass in objectClasses)
				{
					ObjectClassGenerator classGenerator = new ObjectClassGenerator(objectClass, objType, _project, _vault, _command);
					if (classGenerator.Exists)
					{
						_command.WriteWarning(string.Format("File {0} already exists, use -Force to overwrite.", classGenerator.FilePath));
						return false;
					}
				}
			}

			ObjTypes valueLists = Vault.ValueListOperations.GetValueLists();
			foreach (ObjType valueList in valueLists)
			{
				if (valueList.RealObjectType)
					continue;
				ValueListGenerator vlGenerator = new ValueListGenerator(valueList, _project, _vault);

				if (vlGenerator.Exists)
				{
					_command.WriteWarning(string.Format("File {0} already exists, use -Force to overwrite.", vlGenerator.FilePath));
					return false;
				}
			}

			return true;
		}

		public void Generate()
		{
			_command.WriteLine("Beginning generation.");

			CreateBaseObjType();

			PropertyDefGenerator pdefGenerator = new PropertyDefGenerator(_project, _vault);
			if (pdefGenerator.Exists)
			{
				_command.WriteLine(string.Format("Adding {0} to project.", pdefGenerator.FilePath));
				_project.AddFile(pdefGenerator.FilePath, pdefGenerator.GenerateCode());
			}

			ObjTypes objTypes = Vault.ObjectTypeOperations.GetObjectTypes();

			foreach (ObjType objType in objTypes)
			{
				ObjTypeGenerator otGenerator = new ObjTypeGenerator(objType, _project);
				
				_command.WriteLine(string.Format("Adding {0} to project.", otGenerator.FilePath));

				_project.AddFile(otGenerator.FilePath, otGenerator.GenerateObjTypeCode());

				ObjectClasses objectClasses = Vault.ClassOperations.GetObjectClasses(objType.ID);

				foreach (ObjectClass objectClass in objectClasses)
				{
					ObjectClassGenerator classGenerator = new ObjectClassGenerator(objectClass, objType, _project, _vault, _command);

					_command.WriteLine(string.Format("Adding {0} to project.", classGenerator.FilePath));

					_project.AddFile(classGenerator.PartialsFilePath, classGenerator.GenerateClassCode());
					_project.AddFile(classGenerator.FilePath, classGenerator.GenerateClassCode(true));
				}
			}

			ObjTypes valueLists = Vault.ValueListOperations.GetValueLists();

			foreach (ObjType valueList in valueLists)
			{
				if(valueList.RealObjectType)
					continue;
				ValueListGenerator vlGenerator = new ValueListGenerator(valueList, _project, _vault);

				_command.WriteLine(string.Format("Adding {0} to project.", vlGenerator.FilePath));

				_project.AddFile(vlGenerator.FilePath, vlGenerator.GenerateValueListCode());
			}
		}

		public void CreateBaseObjType()
		{
			ObjVerExGenerator baseGenerator = new ObjVerExGenerator(_project);
			_project.AddFile(ObjVerExGenerator.FilePath, baseGenerator.GenerateBaseObjTypeCode());

			List<string> templates = new List<string>
			{
				"LookupsExtensionMethods.cs",
				"MFIdentifier.cs",
				"MFUtils.cs",
				"PropertyValuesExtensionMethods.cs",
				"VaultExtensionMethods.cs"
			};

			TemplateManager.GenerateTemplates(templates, _project);
		}
	}
}
