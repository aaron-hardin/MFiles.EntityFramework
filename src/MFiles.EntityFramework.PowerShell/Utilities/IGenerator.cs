using EnvDTE;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	internal interface IGenerator
	{
		Project Project { get; set; }

		bool Exists { get; }

		string FilePath { get; }

		string GenerateCode(bool partial = false);

		void Update();

		void CreateNew();
	}
}
