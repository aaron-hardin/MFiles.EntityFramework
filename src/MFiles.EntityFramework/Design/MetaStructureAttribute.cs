using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MFilesAPI;

namespace MFiles.EntityFramework.Design
{
	/// <summary>
	/// Allows parameters to be given information for generating as structure.
	/// </summary>
	[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
	public class MetaStructurePropertyAttribute : Attribute
	{
		public MFDataType DataType { get; set; }
		public string Name { get; set; }
		public bool Exclude { get; set; }
		public string Alias { get; set; }
		public bool Required { get; set; }

		public MetaStructurePropertyAttribute()
		{
			DataType = MFDataType.MFDatatypeUninitialized;
			Name = "";
			Exclude = false;
			Alias = "";
			Required = false;
		}
	}

	/// <summary>
	/// Allows parameters to be given information for generating as structure.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class MetaStructureAssociatedPropertyAttribute : Attribute
	{
		public int PropertyDef { get; set; }
		public bool Required { get; set; }

		public MetaStructureAssociatedPropertyAttribute()
		{
			PropertyDef = -1;
			Required = false;
		}
	}

	/// <summary>
	/// Allows classes to be given information for generating as structure.
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
	public class MetaStructureClassAttribute : Attribute
	{
		public string Identifier { get; set; }
		public string Name { get; set; }
		public bool TopLevelClass { get; set; }
		public string NamingConvention { get; set; }
		public string ClassAlias { get; set; }
		public string ObjTypeAlias { get; set; }
		public bool ShowInTaskPane { get; set; }
	    public bool CanHaveFiles { get; set; }

		public MetaStructureClassAttribute()
		{
			Identifier = "";
			Name = "";
			TopLevelClass = false;
			NamingConvention = "";
			ClassAlias = "";
			ObjTypeAlias = "";
			ShowInTaskPane = false;
		    CanHaveFiles = false;
		}

		public string ExpandNamingConvention(Vault vault)
		{
			string expandedText = NamingConvention;

			// Loop over any property placeholders found.
			MatchCollection matches = Regex.Matches( NamingConvention,
				@"%%(.+?)%%", RegexOptions.IgnoreCase );
			foreach( Match match in matches )
			{
				string subName = match.Groups[ 0 ].Value.Substring( 2, match.Groups[ 0 ].Length - 4 );
				string namingAlias = VaultHelpers.GetPropertyDefAliasWithDataType( subName, MFDataType.MFDatatypeLookup );
				int namingPropId = vault.PropertyDefOperations.GetPropertyDefIDByAlias( namingAlias );

				expandedText = expandedText.Replace( match.Groups[ 0 ].Value, namingPropId.ToString( CultureInfo.InvariantCulture ) );
			}

			return expandedText;

		}
	}

	/// <summary>
	/// Allows parameters to be given information for generating as structure.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class MetaStructureObjectTypeAttribute : Attribute
	{
		public string NameSingular { get; set; }
		public string NamePlural { get; set; }
		
		public MetaStructureObjectTypeAttribute()
		{
			NameSingular = "";
			NamePlural = "";
		}
	}
}
