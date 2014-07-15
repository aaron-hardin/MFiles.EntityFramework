using System.IO;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Extensions;
using MFiles.EntityFramework.PowerShell.Templates;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	public class ObjVerExGenerator
	{
		private readonly Project _project;

		public ObjVerExGenerator(Project project)
		{
			_project = project;
		}

		public static string FilePath
		{
			get { return Path.Combine("Models", "ObjVerEx.cs"); }
		}

		public bool Exists()
		{
			string absolutePath = Path.Combine(_project.GetProjectDir(), FilePath);
			return File.Exists(absolutePath);
		}

		public string GenerateBaseObjTypeCode()
		{
			string code = TemplateManager.ReadTemplate("ObjVerEx.cs");

			code = code.Replace("NAMESPACE", _project.GetModelNamespace());

			return code;
		}
	}
}
