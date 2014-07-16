using System;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Extensions;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	internal class ModelGenerator
	{
		private readonly Project _project;
		private readonly string _password;
		private readonly bool _force;
		private readonly MigrationsDomainCommand _command;
		private Vault _vault;

		public ModelGenerator(MigrationsDomainCommand command, string password, bool force)
		{
			_command = command;
			_project = command.Project;
			_password = password;
			_force = force;

			if (!IsValid())
			{
				_command.WriteWarning("Configuration invalid.");
				throw new Exception("");
			}
		}

		private Vault Vault
		{
			get
			{
				if (_vault == null)
				{
					MFilesServerApplication server = new MFilesServerApplication();
					server.Connect(MFAuthType.MFAuthTypeSpecificWindowsUser, "Administrator", _password, "aaronserver",
						NetworkAddress: "aaronserver");
					_vault = server.LogInToVault("{1B4F147E-FAC7-4E0F-80B7-D5340A15A5B2}");
				}
				return _vault;
			}
		}

		public bool IsValid()
		{
			if (_force)
				return true;

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

					_project.AddFile(classGenerator.FilePath, classGenerator.GenerateClassCode());
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
		}
	}
}
