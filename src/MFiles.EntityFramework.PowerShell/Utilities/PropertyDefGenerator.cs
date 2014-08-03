using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Extensions;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	internal class PropertyDefGenerator : IGenerator
	{
		private Project _project;
		private readonly Vault _vault;
		private readonly MigrationsDomainCommand _command;

		public PropertyDefGenerator(Project project, Vault vault, MigrationsDomainCommand command)
		{
			_project = project;
			_vault = vault;
			_command = command;
		}

		public bool Exists
		{
			get
			{
				string absolutePath = Path.Combine(_project.GetProjectDir(), FilePath);
				return File.Exists(absolutePath);
			}
		}

		public string FilePath
		{
			get { return Path.Combine("Models", "PropertyDefinitions.cs"); }
		}

		public Project Project
		{
			get { return _project; }
			set { _project = value; }
		}

		public string GenerateCode(bool partial = false)
		{
			_command.WriteVerbose("Generating code");

			CodeTypeDeclaration targetClass = new CodeTypeDeclaration("PropertyDefinitions")
			{
				IsEnum = true,
				TypeAttributes = TypeAttributes.Public | TypeAttributes.Abstract
			};

			_command.WriteVerbose("Getting properties");

			PropertyDefsAdmin props = _vault.PropertyDefOperations.GetPropertyDefsAdmin();
			foreach (PropertyDefAdmin prop in props)
			{
				// Creates the enum member
				CodeMemberField f = new CodeMemberField("PropertyDefinitions", prop.PropertyDef.Name.CleanName());

				// Adds the description attribute
				f.CustomAttributes.Add(new CodeAttributeDeclaration("Description", new CodeAttributeArgument(new CodePrimitiveExpression(prop.PropertyDef.Name))));

				targetClass.Members.Add(f);
			}

			_command.WriteVerbose("Creating Namespace");

			System.CodeDom.CodeNamespace targetNamespace = new System.CodeDom.CodeNamespace(_project.GetModelNamespace());
			targetNamespace.Types.Add(targetClass);

			_command.WriteVerbose("Building string");

			StringBuilder sbCode = new StringBuilder();
			using (StringWriter sw = new StringWriter(sbCode))
			{
				CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
				CodeGeneratorOptions options = new CodeGeneratorOptions { BracingStyle = "C" };

				provider.GenerateCodeFromNamespace(targetNamespace, sw, options);
			}

			string[] namespaces = { "System.ComponentModel" };
			string usingSt = namespaces.Aggregate("", (current, ns) => current + string.Format("using {0};\r\n", ns));

			string code = string.Format("{0}\r\n{1}", usingSt, sbCode);

			return code;
		}

		public void Update()
		{
			throw new System.NotImplementedException();
		}

		public void CreateNew()
		{
			throw new System.NotImplementedException();
		}
	}
}
