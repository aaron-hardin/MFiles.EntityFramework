// This file comes from Microsoft's Entity Framework and is covered under their license.
// See LICENSE.txt

using System;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace MFiles.EntityFramework.PowerShell.Extensions
{
	internal static class StringExtensions
	{
		private static readonly Regex _migrationIdPattern = new Regex("\\d{15}_.+");

		static StringExtensions()
		{
		}

		public static bool EqualsIgnoreCase(this string s1, string s2)
		{
			return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
		}

		public static string MigrationName(this string migrationId)
		{
			return migrationId.Substring(16);
		}

		public static bool IsValidMigrationId(this string migrationId)
		{
			if (!_migrationIdPattern.IsMatch(migrationId))
				return migrationId == "0";
			return true;
		}

		public static string CleanName(this string name)
		{
			//Compliant with item 2.4.2 of the C# specification
			var regex =
				new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
			string ret = regex.Replace(name, "");

			//The identifier must start with a character or a "_"
			if (!char.IsLetter(ret, 0) || !CodeDomProvider.CreateProvider("C#").IsValidIdentifier(ret))
			{
				ret = string.Concat("_", ret);
			}

			return ret;
		}
	}
}