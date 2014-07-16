using System;
using System.IO;
using System.Reflection;

namespace testpkg.PowerShell.Templates
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
	}
}
