using System;
using System.Configuration;
using MFiles.EntityFramework.PowerShell.Models;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	static class SettingLoader
	{
		public static VaultConnectionSettings LoadConnectionSettings(this KeyValueConfigurationCollection settings, string password)
		{
			return new VaultConnectionSettings(settings.LoadAuthType(), settings.LoadUsername(), password, settings.LoadVaultGuid(),
					settings.LoadDomain(), settings.LoadAddress(), settings.LoadProtocol(), settings.LoadPort());
		}

		private static int LoadAuthType(this KeyValueConfigurationCollection settings)
		{
			string value = LoadSetting(settings, "AuthType");

			if (value == null)
				return 1;  // default = MFAuthTypeLoggedOnWindowsUser

			int result;
			bool success = int.TryParse(value, out result);

			return success ? result : 1;  // default = MFAuthTypeLoggedOnWindowsUser
		}

		private static string LoadUsername(this KeyValueConfigurationCollection settings)
		{
			return LoadSetting(settings, "Username") ?? "";  // default = ""
		}

		private static string LoadVaultGuid(this KeyValueConfigurationCollection settings)
		{
			// TODO: ensure that the VaultGuid is not null ahead of time.
			string value = LoadSetting(settings, "VaultGuid");
			if (value == null)
				throw new Exception("VaultGuid Setting is required.");
			return value;
		}

		private static string LoadDomain(this KeyValueConfigurationCollection settings)
		{
			return LoadSetting(settings, "Domain") ?? "";  // default = ""
		}

		private static string LoadAddress(this KeyValueConfigurationCollection settings)
		{
			return LoadSetting(settings, "Address") ?? "localhost";  // default = "localhost"
		}

		private static ProtocolSequence.Protocol LoadProtocol(this KeyValueConfigurationCollection settings)
		{
			string value = LoadSetting(settings, "Protocol");

			if (value == null)
				return ProtocolSequence.Protocol.TCPIP;  // default

			try
			{
				return value.GetValueFromDescription<ProtocolSequence.Protocol>();
			}
			catch
			{
				return ProtocolSequence.Protocol.TCPIP;  // default
			}
		}

		private static int LoadPort(this KeyValueConfigurationCollection settings)
		{
			string value = LoadSetting(settings, "Port");

			if (value == null)
				return 2266;  // default = 2266

			int result;
			bool success = int.TryParse(value, out result);

			return success ? result : 2266;  // default = "2266"
		}

		private static string LoadSetting(this KeyValueConfigurationCollection settings, string settingName)
		{
			return settings[settingName] != null ? settings[settingName].Value : null;
		}
	}
}
