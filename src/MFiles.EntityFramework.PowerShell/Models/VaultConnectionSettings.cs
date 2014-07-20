using System.Globalization;
using System.Reflection;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell.Models
{
	public class VaultConnectionSettings
	{
		public MFAuthType AuthType { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Domain { get; set; }
		public string NetworkAddress { get; set; }
		public ProtocolSequence.Protocol Protocol { get; set; }
		public string Port { get; set; }
		public string VaultGuid { get; set; }

		public VaultConnectionSettings(int authType, string username, string password, string vaultGuid, string domain = "",
			string networkAddress = "localhost", ProtocolSequence.Protocol protocol = ProtocolSequence.Protocol.TCPIP, int port = 2266)
		{
			AuthType = (MFAuthType) authType;
			Username = username;
			Password = password;
			VaultGuid = vaultGuid;
			Domain = domain;
			NetworkAddress = networkAddress;
			Protocol = protocol;
			Port = port.ToString(CultureInfo.InvariantCulture);
		}

		public Vault GetServerVault()
		{
			MFilesServerApplication server = new MFilesServerApplication();
			server.Connect(AuthType, Username, Password, Domain, Protocol.GetDescription(), NetworkAddress, Port);
			return server.LogInToVault(VaultGuid);
		}
	}
}
