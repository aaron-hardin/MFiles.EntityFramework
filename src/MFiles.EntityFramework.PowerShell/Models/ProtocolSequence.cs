using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MFiles.EntityFramework.PowerShell.Models
{
	public static class ProtocolSequence
	{
		public enum Protocol
		{
			[Description("ncacn_ip_tcp")]
			TCPIP,
			[Description("ncacn_spx")]
			SPX,
			[Description("ncalrpc")]
			LPC,
			[Description("ncacn_http")]
			HTTP
		};

		public static string GetDescription(this Enum value)
		{
			FieldInfo field = value.GetType().GetField(value.ToString());

			DescriptionAttribute attribute
					= Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
						as DescriptionAttribute;

			return attribute == null ? value.ToString() : attribute.Description;
		}

		public static T GetValueFromDescription<T>(this string description)
		{
			var type = typeof(T);
			if (!type.IsEnum) throw new InvalidOperationException();
			foreach (var field in type.GetFields())
			{
				var attribute = Attribute.GetCustomAttribute(field,
					typeof(DescriptionAttribute)) as DescriptionAttribute;
				if (attribute != null)
				{
					if (attribute.Description == description)
						return (T)field.GetValue(null);
				}
				else
				{
					if (field.Name == description)
						return (T)field.GetValue(null);
				}
			}
			throw new ArgumentException("Not found.", "description");
		}
	}
}
