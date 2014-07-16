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
	public class ValueListGenerator
	{
		private readonly Project _project;
		private readonly ObjType _objType;
		private readonly Vault _vault;

		public ValueListGenerator(ObjType objType, Project project, Vault vault)
		{
			_objType = objType;
			_project = project;
			_vault = vault;
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
			get { return Path.Combine("Models", _objType.NameSingular.CleanName() + ".cs"); }
		}

		public string GenerateValueListCode()
		{
			CodeTypeDeclaration targetClass = new CodeTypeDeclaration(_objType.NameSingular.CleanName())
			{
				IsEnum = true,
				TypeAttributes = TypeAttributes.Public | TypeAttributes.Abstract
			};

			ValueListItems items = _vault.ValueListItemOperations.GetValueListItems(_objType.ID, true);
			foreach (ValueListItem item in items)
			{
				// Creates the enum member
				CodeMemberField f = new CodeMemberField(_objType.NameSingular.CleanName(), item.Name.CleanName());

				// Adds the description attribute
				f.CustomAttributes.Add(new CodeAttributeDeclaration("Description", new CodeAttributeArgument(new CodePrimitiveExpression(item.Name))));

				targetClass.Members.Add(f);
			}
			
			System.CodeDom.CodeNamespace targetNamespace = new System.CodeDom.CodeNamespace(_project.GetModelNamespace());
			targetNamespace.Types.Add(targetClass);

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
	}
}
