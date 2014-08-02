using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	public class ProjectUtilities
	{
		public delegate void WriteLine(string text);

		public static void WriteElements(Project project, CodeElements elements, WriteLine writeLine, int level = 0)
		{
			writeLine("Getting elements");
			foreach (CodeElement element in elements)
			{
				string tabs = "";
				for (int i = 0; i < level; ++i)
				{
					tabs += "\t";
				}
				writeLine(string.Format("{0}{1}: {2}", tabs, element.Kind, element.Name));
				if (element.Kind == vsCMElement.vsCMElementClass)
				{
					CodeClass myClass = (CodeClass)element;
					// do stuff with that class here
					writeLine(tabs + "Class: " + myClass.FullName);

					WriteElements(project, myClass.Members, writeLine, level + 1);
				}
				if (element.Kind == vsCMElement.vsCMElementNamespace)
				{
					bool myProject = element.Name == project.Name;

					if (myProject == false && level == 0)
						continue;
					CodeNamespace cnm = (CodeNamespace)element;

					WriteElements(project, cnm.Members, writeLine, level + 1);
				}
			}
		}
	}
}
