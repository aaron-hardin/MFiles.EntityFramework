using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE80;
using MFiles.EntityFramework.PowerShell.Templates;
using MFiles.EntityFramework.PowerShell.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NUnit.Framework;
using EnvDTE;

namespace Test
{
	[TestFixture]
    public class MiscTests
    {
		[Test]
		public void GetTemplate()
		{
			string code = TemplateManager.ReadTemplate("ObjVerEx.cs").Replace("NAMESPACE", "test.testNS.Models");
			Console.WriteLine(code);
		}

		[Test]
		public void WriteElementsTest()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			Console.WriteLine(assembly.FullName);
			assembly = Assembly.GetAssembly(typeof (ProjectUtilities));
			Console.WriteLine(assembly.FullName);
			
			//Type t = Type.GetTypeFromProgID("VisualStudio.DTE.10.0");
			//object obj = Activator.CreateInstance(t, true);
			//DTE dte = (DTE)obj;

			//Solution solution = dte.Solution;
			//if(solution == null)
			//	Assert.Fail("Solution is null.");
			//const string solutionPath = @"";
			//if (File.Exists(solutionPath))
			//{
			//	solution.Open(solutionPath);
			//}
			//else
			//{
			//	throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, "Solution file not found at {0}", Path.GetFullPath(solutionPath)));
			//}

			//Project proj = GetActiveProject(dte);
			//ProjectUtilities.WriteElements(proj, proj.CodeModel.CodeElements, Console.WriteLine);
		}

		internal static Project GetActiveProject()
		{
			DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;
			return GetActiveProject(dte);
		}

		internal static Project GetActiveProject(DTE dte)
		{
			Project activeProject = null;

			Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
			if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
			{
				activeProject = activeSolutionProjects.GetValue(0) as Project;
			}

			return activeProject;
		}
    }
}
