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
	public class ObjTypeGenerator
	{
		private readonly Project _project;
		private readonly ObjType _objType;

		public ObjTypeGenerator(ObjType objType, Project project)
		{
			_objType = objType;
			_project = project;
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
			get { return Path.Combine("Models\\BaseTypes", "OT_"+_objType.NameSingular.CleanName() + ".cs"); }
		}

		public string GenerateObjTypeCode()
		{
			CodeTypeDeclaration targetClass = new CodeTypeDeclaration("OT_" + _objType.NameSingular.CleanName())
			{
				IsClass = true,
				TypeAttributes = TypeAttributes.Public | TypeAttributes.Abstract
			};
			targetClass.BaseTypes.Add("ObjVerEx");

			Common.AddConstructors(targetClass);

			System.CodeDom.CodeNamespace targetNamespace = new System.CodeDom.CodeNamespace(_project.GetModelNamespace());
			targetNamespace.Types.Add(targetClass);

			StringBuilder sbCode = new StringBuilder();

			using (StringWriter sw = new StringWriter(sbCode))
			{
				CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
				CodeGeneratorOptions options = new CodeGeneratorOptions { BracingStyle = "C" };

				provider.GenerateCodeFromNamespace(targetNamespace, sw, options);
			}

			string[] namespaces = { "System", "MFilesAPI" };
			string usingSt = namespaces.Aggregate("", (current, ns) => current + string.Format("using {0};\r\n", ns));

			string code = string.Format("{0}\r\n{1}", usingSt, sbCode);

			return code;
		}
	}
}
