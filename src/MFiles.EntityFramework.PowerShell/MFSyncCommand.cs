﻿// This file is based loosely on Microsoft's Entity Framework.
// See LICENSE.txt

using System;
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
			ModelGenerator generator = new ModelGenerator(this, name, force);
			generator.Generate();

			//if (System.Diagnostics.Debugger.IsAttached == false)
			//	System.Diagnostics.Debugger.Launch();


		}
	}
}