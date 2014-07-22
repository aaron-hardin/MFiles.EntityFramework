using System;
using MFiles.EntityFramework.PowerShell.Models;

namespace MFiles.EntityFramework.PowerShell
{
	class MFDiffCommand : MigrationsDomainCommand
	{
		public MFDiffCommand(int mode)
		{
			MFDiffCommand migrationCommand = this;
			Execute(() => migrationCommand.Execute(mode));
		}

		[STAThread]
		public void Execute(int mode)
		{
			WriteVerbose("Starting diff with mode: " + mode);
			DiffMode diffMode = (DiffMode) mode;

			switch (diffMode)
			{
				case DiffMode.ServerAndLocal:
					WriteWarning("Functionality not completed.");
					break;
				case DiffMode.ServerAndModels:
					WriteWarning("Functionality not completed.");
					break;
				case DiffMode.LocalAndModels:
					WriteWarning("Functionality not completed.");
					break;
				default:
					throw new NotImplementedException("DiffMode not implemented.");
			}
		}
	}
}
