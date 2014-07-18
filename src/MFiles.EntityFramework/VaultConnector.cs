using System.Configuration;

namespace MFiles.EntityFramework
{
	public class VaultConnector
	{
		public static string GetSettings()
		{
			string text = ConfigurationManager.AppSettings["MFSetting"];
			return text;
		}
	}
}
