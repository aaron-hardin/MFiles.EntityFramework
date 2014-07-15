using System;
using MFilesAPI;

namespace testpkg.Design
{
	public class VaultHelpers
	{
		/// <summary>
		/// Creates a PropertyDef
		/// </summary>
		/// <param name="vault">vault to create propertydef in</param>
		/// <param name="dataType">datatype to create PropertyDef with</param>
		/// <param name="name">Name to use for PD</param>
		/// <param name="alias">Alias(es) to use for PD, default blank</param>
		/// <param name="valueList">Value list, used for Lookup and MultiSelectLookup</param>
		/// <returns></returns>
		public static int CreatePropertyDef(Vault vault, MFDataType dataType, string name, string alias = "", int valueList = -1)
		{
			PropertyDefAdmin newProp = new PropertyDefAdmin
			{
				SemanticAliases = { Value = alias },
				PropertyDef = { Name = name, DataType = dataType }
			};

			if( dataType == MFDataType.MFDatatypeLookup || dataType == MFDataType.MFDatatypeMultiSelectLookup )
			{
				if( valueList == -1 )
					throw new ArgumentException( "CreatePropertyDef requires int valueList for lookups" );
				newProp.PropertyDef.BasedOnValueList = true;
				newProp.PropertyDef.ValueList = valueList;
			}

			return vault.PropertyDefOperations.AddPropertyDefAdmin( newProp ).PropertyDef.ID;
		}

		/// <summary>
		/// Generates alias using standard convention, appends the datatype
		/// </summary>
		/// <param name="propName"></param>
		/// <param name="dataType"></param>
		/// <returns></returns>
		public static string GetPropertyDefAliasWithDataType( string propName, MFDataType dataType )
		{
			return string.Format( "{0} ({1})", GetPropertyDefAlias(propName), dataType );
		}

		/// <summary>
		/// Generates alias using standard convention
		/// </summary>
		/// <param name="propName"></param>
		/// <returns></returns>
		public static string GetPropertyDefAlias(string propName)
		{
			return string.Format( "M-Files.Property.{0}", propName );
		}

		public static string GetObjTypeAlias( string name )
		{
			return string.Format( "M-Files.Object.{0}", name );
		}

		public static string GetClassAlias( string name )
		{
			return string.Format( "M-Files.Class.{0}", name );
		}
	}
}
