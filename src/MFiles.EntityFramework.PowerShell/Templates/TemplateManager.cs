using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Extensions;

namespace MFiles.EntityFramework.PowerShell.Templates
{
	public static class TemplateManager
	{
		public static string ReadTemplate(string name)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			string resourceName = typeof (TemplateManager).Namespace + "." + name;

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				if(stream == null)
					throw new Exception("Stream is null for "+resourceName);
				using (StreamReader reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public static string ReadTemplate(string name, string @namespace)
		{
			string code = ReadTemplate(name);
			return code.Replace("NAMESPACE", @namespace);
		}

		public static void GenerateTemplates(List<string> templates, Project project)
		{
			const string basepath = "Models";
			foreach (string template in templates)
			{
				string path = Path.Combine(basepath, template);
				project.AddFile(path, ReadTemplate(template, project.GetModelNamespace()));
			}
		}
	}
}
