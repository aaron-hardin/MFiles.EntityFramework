using System;
using System.Text;
using MFilesAPI;

namespace MFiles.QMS
{

	/// <summary>
	/// Provides convienince extension methods for the PropertyValues collection.
	/// </summary>
	public static class PropertyValuesExtensionMethods
	{

		/// <summary>
		/// Checks whether an object has a specific property.
		/// </summary>
		/// <param name="propId">The PropertyDef ID.</param>
		/// <returns>Returns true if the property was found.</returns>
		public static bool Exists(this PropertyValues props, int propID)
		{
			return (props.IndexOf(propID) != -1);
		}

		/// <summary>
		/// Checks whether an object has a specific property and it's vaule is not null.
		/// </summary>
		/// <param name="propId">The PropertyDef ID.</param>
		/// <returns>Returns true if the property was found and has a value.</returns>
		public static bool HasValue(this PropertyValues props, int propID)
		{
			// Try to get the property
			PropertyValue pv = props.GetProperty(propID);

			// Return whether it was found and has a value.
			return (pv != null && !pv.Value.IsNULL());
		}


		/// <summary>
		/// Checks whether an object has a specific boolean property and it is true.
		/// </summary>
		/// <param name="propID">The PropertyDef ID.</param>
		/// <returns>
		///  Returns defaultValue if the property was not found, 
		///  if it was not boolean, or was not set to true.
		/// </returns>
		public static bool HasFlag(this PropertyValues props, int propID, bool defaultValue = false)
		{
			// Try to get the property
			PropertyValue pv = props.GetProperty(propID);

			if (pv != null)
			{
				// The property was found
				if (!pv.Value.IsNULL() && pv.Value.DataType == MFDataType.MFDatatypeBoolean)

					// The value isn't null, and it's boolean; return it's value.
					return (bool)pv.Value.Value;
			}

			// If we made it here, return default value.
			return defaultValue;
		}

		/// <summary>
		/// Returns the specified object property if found.
		/// </summary>
		/// <param name="propID">The PropertyDef id of the property to look for.</param>
		/// <returns>Returns null if not found.</returns>
		public static PropertyValue GetProperty(this PropertyValues props, int propID)
		{
			int i = props.IndexOf(propID);

			if (i == -1)
				return null;
			else
				return props[i];
		}


		/// <summary>
		/// Tries to retreive the specified property value from the collection.
		/// </summary>
		/// <param name="props">Source property values</param>
		/// <param name="propID">PropertyDef ID</param>
		/// <param name="propVal">The value to populate if the value is found.</param>
		/// <returns>Return true if the propVal parameter was set successfully.</returns>
		public static bool TryGetProperty(this PropertyValues props, int propID, out PropertyValue propVal)
		{
			propVal = null;

			int i = props.IndexOf(propID);

			if (i == -1)
			{
				propVal = null;
				return false;
			}
			else
			{
				propVal = props[i];
				return true;
			}
		}


		/// <summary>
		/// Adds or updates the specificed property in the property values collection with the passed value.
		/// </summary>
		/// <param name="props">The source property values in which to set the property.</param>
		/// <param name="propID">The propertyDef of the PropertyValue</param>
		/// <param name="dataType">The datatype of the PropertyValue</param>
		/// <param name="value">The value of the PropertyValue</param>
		/// <returns>The actual PropertyValue created/added to the propertyValues collection.</returns>
		public static PropertyValue SetProperty(this PropertyValues props, int propID, MFDataType dataType, object value)
		{
			PropertyValue pv = new PropertyValue();
			pv.PropertyDef = propID;

			// Can't rely on implicit conversion to int, when passed to API as a variant, so we do it explicitly now.
			if (value is MFIdentifier)
				value = ((MFIdentifier)value).ID;

			if (value != null)
				pv.Value.SetValue(dataType, value);
			else
				pv.Value.SetValueToNULL(dataType);

			return props.SetProperty(pv);

		}

		/// <summary>
		/// Adds or updates the specificed property in the property values collection with the passed value
		/// </summary>
		/// <param name="props">The source property values in which to set the property.</param>
		/// <param name="propID">The propertyDef of the PropertyValue</param>
		/// <param name="value">The typedvalue of the PropertyValue</param>
		/// <returns>The actual PropertyValue created/added to the propertyValues collection.</returns>
		public static PropertyValue SetProperty(this PropertyValues props, int propID, TypedValue value)
		{
			PropertyValue pv = new PropertyValue { PropertyDef = propID, Value = value };
			return props.SetProperty(pv);
		}

		/// <summary>
		/// Adds or updates the specificed property in the property values collection.
		/// </summary>
		/// <param name="props">The source property values in which to set the property.</param>
		/// <param name="propVal">The property value to add to the collection.</param>
		/// <returns>The actual PropertyValue created/added to the propertyValues collection.</returns>
		public static PropertyValue SetProperty(this PropertyValues props, PropertyValue propVal)
		{
			if (propVal == null)
				throw new ArgumentNullException("propVal");

			// Get the current index of the property, if it exists.
			int index = props.IndexOf(propVal.PropertyDef);

			// Remove the old property if it existed.
			props.RemoveProperty(propVal.PropertyDef);

			// Add a clone of the new property to the same index.
			PropertyValue pv = propVal.Clone();
			props.Add(index, pv);

			// Return the property.
			return pv;
		}

		/// <summary>
		/// Removes the specified property from the property values collection.
		/// </summary>
		/// <param name="props">The source property values from which to remove the property.</param>
		/// <param name="propID">The property to be removed.</param>
		/// <returns>The removed propertyValue (if any).</returns>
		public static PropertyValue RemoveProperty(this PropertyValues props, int propID)
		{
			PropertyValue pv = null;

			int index = props.IndexOf(propID);
			if (index != -1)
			{
				pv = props[index];
				props.Remove(index);
			}

			return pv;
		}

		/// <summary>
		/// Adds the item to the specified multiselectlookup property.
		/// </summary>
		/// <param name="props">The source property values from which to remove the property.</param>
		/// <param name="propID">The property which to add the lookup to.</param>
		/// <param name="item">The item to be added as a lookup</param>
		/// <param name="version">The specific version of the item for the lookup to reference.</param>
		/// <returns>True if the property value was created or changed.</returns>
		public static bool AddLookup(this PropertyValues props, int propID, int item, int version = -1)
		{
			// Try to find existing property value.
			PropertyValue pv = props.GetProperty(propID);

			// If a property doesn't already exist, create it.
			if (pv == null)
			{
				pv = new PropertyValue();
				pv.PropertyDef = propID;
				pv.Value.SetValueToNULL(MFDataType.MFDatatypeMultiSelectLookup);
			}

			// Check if it already has the item.
			Lookups lookups = pv.Value.GetValueAsLookups();
			int match = lookups.GetLookupIndexByItem(item);
			if (match == -1 || lookups[match].Version != version)
			{
				// The items isn't in the list already.

				// Add the item to the list and save.
				Lookup lookup = new Lookup();
				lookup.Item = item;
				lookup.Version = version;
				lookups.Add(-1, lookup);
				pv.Value.SetValueToMultiSelectLookup(lookups);
				props.SetProperty(pv);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Adds the item to the specified multiselectlookup property.
		/// </summary>
		/// <param name="props">The source property values from which to add the property.</param>
		/// <param name="propID">The property which to add the lookup to.</param>
		/// <param name="item">The item to be added as a lookup</param>
		/// <param name="version">Indicates whether the exact or latest version should be used.</param>
		/// <returns>True if the property value was created or changed.</returns>
		public static bool AddLookup(this PropertyValues props, int propID, ObjVer item, bool exactVersion = false)
		{
			return props.AddLookup(propID, item.ID, (exactVersion) ? item.Version : -1);
		}

		/// <summary>
		/// Removes the item from the specified multiselectlookup property.
		/// </summary>
		/// <param name="props">The source property values from which to remove the property.</param>
		/// <param name="propID">The property from which the lookup should be removed.</param>
		/// <param name="item">The item to remove from the lookups.</param>
		/// <returns>Returns true if the property value was altered.</returns>
		public static bool RemoveLookup(this PropertyValues props, int propID, int item)
		{
			// Try to find existing property value.
			PropertyValue pv = props.GetProperty(propID);

			// Only proceed if we found an existing property value.
			if (pv != null)
			{
				// Property value exists.
				// Try to find passed item in lookups.
				Lookups lookups = pv.Value.GetValueAsLookups();
				int index = lookups.GetLookupIndexByItem(item);
				if (index != -1)
				{
					// Lookup item exists. Remove it, and save the results.
					lookups.Remove(index);
					pv.Value.SetValueToMultiSelectLookup(lookups);
					props.SetProperty(pv);
					return true;
				}
			}

			// Return null or unmodified property value.
			return false;
		}

		/// <summary>
		/// Returns a string representation of the property values. For debugging.
		/// </summary>
		/// <param name="props">The source property values from which to create the string.</param>
		/// <returns>A string representation of the propertyValues.</returns>
		public static string ToStringEx(this PropertyValues propVals)
		{

			StringBuilder s = new StringBuilder();
			foreach (PropertyValue pv in propVals)
			{
				s.AppendFormat("{0}: {1}\n", pv.PropertyDef, pv.TypedValue.DisplayValue);
			}

			return s.ToString();
		}

	}
}
