using System;
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

		public bool ForceWorkflow { get; set; }
		public int ID { get; set; }
		public int NamePropertyDef { get; set; }
		public int ObjectType { get; set; }
		public bool Predefined { get; set; }
		public string[] SemanticAliases { get; set; }
		public int Workflow { get; set; }

		public MetaStructureClassAttribute()
		{
			Identifier = "ID";
			Name = "";
			ID = -1;
			NamePropertyDef = 0;
		}
	}

	/// <summary>
	/// Allows parameters to be given information for generating as structure.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class MetaStructureObjectTypeAttribute : Attribute
	{
		public string Identifier { get; set; }

		public string NameSingular { get; set; }
		public string NamePlural { get; set; }
		
		public MetaStructureObjectTypeAttribute()
		{
			Identifier = "ObjectType.Guid";
			NameSingular = "";
			NamePlural = "";
		}
	}
}
