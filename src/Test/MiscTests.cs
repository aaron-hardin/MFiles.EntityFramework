using System;
using NUnit.Framework;
using testpkg.PowerShell.Templates;

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
