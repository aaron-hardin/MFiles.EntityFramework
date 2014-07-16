using System;
using MFiles.EntityFramework.PowerShell.Templates;
using NUnit.Framework;

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
    }
}
