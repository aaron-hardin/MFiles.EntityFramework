// This file is based loosely on Microsoft's Entity Framework.
// See LICENSE.txt

using System;
using System.Configuration;
using MFiles.EntityFramework.PowerShell.Utilities;

namespace MFiles.EntityFramework.PowerShell
{
	internal class MFSyncCommand : MigrationsDomainCommand
	{
		public MFSyncCommand()
		{
		}

		public MFSyncCommand(string name, bool force, bool ignoreChanges)
		{
			MFSyncCommand migrationCommand = this;
			base.Execute(() => migrationCommand.Execute(name, force, ignoreChanges));
		}

		[STAThread]
		public void Execute(string name, bool force, bool ignoreChanges)
		{
			//if (System.Diagnostics.Debugger.IsAttached == false)
			//	System.Diagnostics.Debugger.Launch();

			WriteWarning("gonna fail :(");

			try
			{
				string text = ConfigurationManager.AppSettings["MFSetting"];
				if (text == null)
					WriteLine("Setting not found");
				else
					WriteLine("Setting: " + text);
			}
			catch (Exception)
			{
				
				//throw;
			}

			ModelGenerator generator = new ModelGenerator(this, name, force);
			generator.Generate();

			//if (System.Diagnostics.Debugger.IsAttached == false)
			//	System.Diagnostics.Debugger.Launch();


		}
	}
}