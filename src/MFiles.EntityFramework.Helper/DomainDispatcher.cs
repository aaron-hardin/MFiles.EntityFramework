// This file comes from Microsoft's Entity Framework and is covered under their license.
// See LICENSE.txt

using System;
using System.Management.Automation;
using EnvDTE;

namespace MFiles.EntityFramework.PowerShell.Helper
{
	public class DomainDispatcher : MarshalByRefObject
	{
		private readonly PSCmdlet _cmdlet;
		private readonly DTE _dte;

		public DomainDispatcher()
		{
		}

		[CLSCompliant(false)]
		public DomainDispatcher(PSCmdlet cmdlet)
		{
			if (cmdlet == null)
				throw new ArgumentNullException("cmdlet");
			_cmdlet = cmdlet;
			_dte = (DTE) cmdlet.GetVariableValue("DTE");
		}

		public void WriteLine(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new ArgumentNullException("text");
			_cmdlet.Host.UI.WriteLine(text);
		}

		public void WriteWarning(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new ArgumentNullException("text");
			_cmdlet.WriteWarning(text);
		}

		public void WriteVerbose(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new ArgumentNullException("text");
			_cmdlet.WriteVerbose(text);
		}

		public virtual void OpenFile(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				throw new ArgumentNullException("fileName");
			_dte.ItemOperations.OpenFile(fileName, "{00000000-0000-0000-0000-000000000000}");
		}

		public void NewTextFile(string text, string item = "General\\Text File")
		{
			((TextDocument)
				_dte.ItemOperations.NewFile(item, "", "{00000000-0000-0000-0000-000000000000}").Document.Object("TextDocument"))
				.StartPoint.CreateEditPoint().Insert(text);
		}
	}
}