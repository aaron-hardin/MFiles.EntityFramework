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
			get { return Path.Combine("Models", "OT_"+_objType.NameSingular.CleanName() + ".cs"); }
		}

		public string GenerateObjTypeCode()
		{
			CodeTypeDeclaration targetClass = new CodeTypeDeclaration("OT_" + _objType.NameSingular.CleanName())
			{
				IsClass = true,
				TypeAttributes = TypeAttributes.Public | TypeAttributes.Abstract
			};
			targetClass.BaseTypes.Add("ObjVerEx");

			AddConstructors(targetClass);

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

		/// <summary> 
		/// Add constructors to the class. 
		/// </summary> 
		private void AddConstructors(CodeTypeDeclaration targetClass)
		{
			// Declare the constructor
			CodeConstructor constructor = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(Vault), "vault"));
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(ObjectVersion), "versionInfo"));
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("vault"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("versionInfo"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor);

			// Declare the constructor
			CodeConstructor constructor2 = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor2.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(ObjectVersionAndProperties), "ovap"));
			constructor2.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor2.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("ovap"));
			constructor2.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor2);

			// Declare the constructor
			CodeConstructor constructor3 = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor3.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(Vault), "vault"));
			constructor3.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(ObjVer), "objVer"));
			constructor3.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor3.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("vault"));
			constructor3.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("objVer"));
			constructor3.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor3);

			// Declare the constructor
			CodeConstructor constructor4 = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(Vault), "vault"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(int), "objType"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(int), "id"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(int), "version"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("vault"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("objType"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("id"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("version"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor4);
		}
	}
}
