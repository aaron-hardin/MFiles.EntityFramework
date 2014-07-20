using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MFilesAPI;
using System.Text.RegularExpressions;

namespace NAMESPACE
{
	/// <summary>
	/// Wraps an ObjVer object and vault, and provides convenience methods.
	/// </summary>
	public class ObjVerEx : IDisposable, IEquatable<ObjVerEx>
	{
		private bool checkIn;
		private bool propertiesChanged = false;
		private bool readOnly;

		public void Dispose()
		{
			EndRequireCheckedOut(checkIn);
		}

		public virtual int ObjType { get; set; }

		#region Public Properties

		/// <summary>
		/// Returns the vault in which this object resides.
		/// </summary>
		public Vault Vault { get; private set; }

		/// <summary>
		/// Returns the ObjVer of this object.
		/// </summary>
		public ObjVer ObjVer { get; private set; }

		/// <summary>
		/// Returns the ObjID of this object.
		/// </summary>
		public ObjID ObjID
		{
			get { return this.ObjVer.ObjID; }
		}

		/// <summary>
		/// Returns the Type of this object.
		/// </summary>
		public int Type
		{
			get { return this.ObjVer.Type; }
		}

		/// <summary>
		/// Returns the ID of this object.
		/// </summary>
		public int ID
		{
			get { return this.ObjVer.ID; }
		}

		/// <summary>
		/// Returns the Version of this object.
		/// </summary>
		public int Version
		{
			get { return this.ObjVer.Version; }
		}

		/// <summary>
		/// The title of this object version.
		/// </summary>
		public string Title
		{
			get { return this.Info.Title; }
		}

		/// <summary>
		/// Returns the version information associated with this ObjVer.
		/// </summary>
		public ObjectVersion Info
		{
			get
			{
				// Load the info from the vault, if it hasn't been yet.
				if (info == null)
					info = this.Vault.ObjectOperations.GetObjectInfo(this.ObjVer, false, true);

				// Return the information.
				return info;
			}
			protected set
			{
				info = value;
			}
		}

		/// <summary>
		/// Returns the properties associated with this ObjVer.
		/// </summary>
		public PropertyValues Properties
		{
			get
			{
				// Load the properties from the vault, if it hasn't been yet.
				if ( props == null )
				{
					// Get the properties from the server.
					props = this.Vault.ObjectPropertyOperations.GetProperties( this.ObjVer, true );

					// Check if the object is checked in.
					if ( !this.Info.ObjectCheckedOut )
					{
						// The object is checked in, so we drop comment property from the properties, so
						// it doesn't get accidentally duplicated when creating a new version. If the object
						// were checked out, the comment would have been removed automatically and any
						// addition would have been intentional.
						// This approach should be reviewed again later on and updated in QMS 3.0.
						props.RemoveProperty( (int) MFBuiltInPropertyDef.MFBuiltInPropertyDefVersionComment );
					}
				}

				// Return the properties.
				return props;
			}
			protected set
			{
				props = value;
			}
		}
		
		/// <summary>
		/// Returns the object's class ID.
		/// </summary>
		public int Class { get { return this.Info.Class; } }
		
		/// <summary>
		/// Returns the object's workflow. -1 if not set.
		/// </summary>
		public int Workflow { get { return GetLookupID(MFBuiltInPropertyDef.MFBuiltInPropertyDefWorkflow); } }
		
		/// <summary>
		/// Returns the object's state. -1 if not set.
		/// </summary>
		public int State { get { return GetLookupID(MFBuiltInPropertyDef.MFBuiltInPropertyDefState); } }
		
		/// <summary>
		/// Indicates whether this object is an M-Files template.
		/// </summary>
		public bool IsTemplate
		{
			get
			{
				return HasPropertyFlag((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefIsTemplate);
			}
		}
		
		/// <summary>
		/// Retreives the history of this object, with each version wrapped as an ObjVerEx object.
		/// Also ensures the order of the object is from newest to oldest.
		/// Includes all versions, including the called instance.
		/// </summary>
		public IEnumerable<ObjVerEx> History
		{
			get
			{
				if (history == null)
				{
					history = from ObjectVersion ov in this.Vault.ObjectOperations.GetHistory(this.ObjID)
							  orderby ov.ObjVer.Version descending
							  select new ObjVerEx(this.Vault, ov);
				}

				return history;
			}

		}

		/// <summary>
		/// The previous version of this object.
		/// </summary>
		public ObjVerEx PreviousVersion
		{
			get
			{
				// Return null if there is no previous version.
				if (this.Version == 1)
					return null;

				// Wrap the (blindly generated) previous version and return.
				ObjVer old = this.ObjVer.Clone();
				old.Version -= 1;
				return new ObjVerEx(this.Vault, old);
			}
		}
		
		/// <summary>
		/// Determines if this version can be modified.
		/// Specifically determines if this is the latest
		/// version, and is not checked out, or is checked out to this user.
		/// </summary>
		public bool CanModify
		{
			get
			{
				// Resolve the latest version's information.
				ObjectVersion ov = this.Vault.ObjectOperations.GetObjectInfo(this.ObjVer, true, true);

				// If this version isn't the latest or it's checked out to someone else, return false.
				return (this.Version == ov.ObjVer.Version
					&& (!ov.ObjectCheckedOut || ov.ObjectCheckedOutToThisUser));
			}
		}
		
		/// <summary>
		/// Returns this object version's permissions.
		/// </summary>
		public ObjectVersionPermissions Permissions
		{
			get { return this.Vault.ObjectOperations.GetObjectPermissions(this.ObjVer); }
		}

		/// <summary>
		/// Returns this object version's ACL.
		/// </summary>
		/// <returns></returns>
		public AccessControlList ACL
		{
			get { return this.Permissions.AccessControlList; }
		}

		#endregion

		#region Private Fields

		// Cached Property backing members.
		private ObjectVersion info = null;
		private PropertyValues props = null;
		private IEnumerable<ObjVerEx> history = null;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new ObjVerEx Object from an ObjectVersion.
		/// </summary>
		/// <param name="vault">The vault where the M-Files object exists.</param>
		/// <param name="versionInfo">ObjectVersion representing the M-Files object.</param>
		public ObjVerEx(Vault vault, ObjectVersion versionInfo, bool checkOut = false)
			: this(vault, versionInfo.ObjVer, checkOut)
		{
			info = versionInfo;
		}

		/// <summary>
		/// Creates a new ObjVerEx Object from an ObjectVersionAndProperties object.
		/// </summary>
		/// <param name="ovap">ObjectVersionAndProperties representing the M-Files object.</param>
		public ObjVerEx(ObjectVersionAndProperties ovap, bool checkOut = false)
			: this(ovap.Vault, ovap.ObjVer, checkOut)
		{
			info = ovap.VersionData;
			props = ovap.Properties;
		}

		/// <summary>
		/// Creates a new ObjVerEx Object from an ObjVer.
		/// </summary>
		/// <param name="vault">The vault where the M-Files object exists.</param>
		/// <param name="versionInfo">ObjVer representing the M-Files object.</param>
		public ObjVerEx(Vault vault, ObjVer objVer, bool checkOut = false)
			: this(vault, objVer.Type, objVer.ID, objVer.Version, checkOut)
		{
			this.Vault = vault;
			this.ObjVer = objVer;

			if (this.ObjVer.Version == -1)
				loadLatestObjVer();
		}

		/// <summary>
		/// Creates a new ObjVerEx Object.
		/// </summary>
		/// <param name="vault">The vault where the M-Files object exists.</param>
		/// <param name="objType">The object type of the M-Files object.</param>
		/// <param name="id">The id of the M-Files object.</param>
		/// <param name="version">The version of the M-Files object.</param>
		public ObjVerEx(Vault vault, int objType, int id, int version, bool checkOut = false)
		{
			this.Vault = vault;
			this.ObjVer = new ObjVer();
			this.ObjVer.SetIDs(objType, id, version);

			if (version == -1)
				loadLatestObjVer();

			// If readOnly = false then check the object out on creation of ObjVerEx
			if (checkOut)
				checkIn = StartRequireCheckedOut();
			readOnly = !checkOut;
		}


		#endregion

		#region Public Methods

	    /// <summary>
		/// Checks if the passed object type reference matches this object.
		/// </summary>
		/// <param name="objType">A reference to an objType.</param>
		/// <returns>True, if the object is of the type passed.</returns>
		public bool IsType(object objType)
		{
			int id = this.Vault.ResolveID(typeof(ObjType), objType);
			return (this.Type == id);
		}

		/// <summary>
		/// Checks if the passed class reference matches this object.
		/// </summary>
		/// <param name="classRef">A reference to a class.</param>
		/// <returns>True, if the object is of the class passed.</returns>
		public bool HasClass(object classRef)
		{
			int id = this.Vault.ResolveID(typeof(ObjectClass), classRef);
			return (this.Class == id);
		}

		/// <summary>
		/// Checks whether an object has a specific property.
		/// </summary>
		/// <param name="propId">A reference to the PropertyDef to look for.</param>
		/// <returns>Returns true if the property was found.</returns>
		public bool HasProperty(object prop)
		{
			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);
			return this.Properties.Exists(id);
		}

		/// <summary>
		/// Checks whether an object has a specific property and it's vaule is not null.
		/// </summary>
		/// <param name="propId">A reference to the PropertyDef to look for.</param>
		/// <returns>Returns true if the property was found.</returns>
		public bool HasValue(object prop)
		{
			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);
			return this.Properties.HasValue(id);
		}
		
		/// <summary>
		/// Checks whether an object has a specific boolean property and it is true.
		/// </summary>
		/// <param name="propID">A reference to the PropertyDef.</param>
		/// <param name="defaultValue">The value to return, if no value is explicitly set.</param>
		/// <returns>
		///  Returns the defaultValue if the property was not found, 
		///  if it was not boolean, or was not set to true.
		/// </returns>
		public bool HasPropertyFlag(object prop, bool defaultValue = false)
		{
			MFIdentifier propIdentifier = prop as MFIdentifier;
			if (propIdentifier != null)
				return this.Properties.HasFlag(propIdentifier.Resolve(this.Vault, typeof(PropertyDef)), defaultValue);

			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);
			return this.Properties.HasFlag(id, defaultValue);
		}

		/// <summary>
		/// Returns the specified object property if found.
		/// </summary>
		/// <param name="propID">The PropertyDef id of the property to look for.</param>
		/// <returns>Returns null if not found.</returns>
		public PropertyValue GetProperty(object prop)
		{
			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);
			return this.Properties.GetProperty(id);
		}

		/// <summary>
		/// Returns the text representation of the property value... or an empty string if there is no value
		/// </summary>
		/// <param name="propID">The PropertyDef id of the property to look for.</param>
		/// <returns>Returns Empty string if not found.</returns>
		public string TryGetPropertyText(object prop)
		{
		    if (!HasValue(prop))
		    {
		        return "";
		    }
		    return GetPropertyText(prop);
		}
        
        /// <summary>
		/// Returns the text representation of the property value.
		/// </summary>
		/// <param name="propID">The PropertyDef id of the property to look for.</param>
		/// <returns>Returns null if not found.</returns>
		public string GetPropertyText(object prop)
		{
			PropertyValue pv = this.GetProperty(prop);

			if (pv != null)
			{
				if (pv.TypedValue.DataType == MFDataType.MFDatatypeLookup && !pv.TypedValue.IsNULL())
				{
					// Value is a non-null lookup - make sure the text is available.
					PropertyDef pd = this.Vault.PropertyDefOperations.GetPropertyDef(pv.PropertyDef);
					return getLookupText(pd, pv.TypedValue.GetValueAsLookup());
				}
				else if (pv.TypedValue.DataType == MFDataType.MFDatatypeMultiSelectLookup && !pv.TypedValue.IsNULL())
				{
					// Value is a non-null multi-select lookup - make sure the text is available.
					PropertyDef pd = this.Vault.PropertyDefOperations.GetPropertyDef(pv.PropertyDef);
					List<string> vals = new List<string>();
					foreach (Lookup lookup in pv.TypedValue.GetValueAsLookups())
						vals.Add(getLookupText(pd, lookup));

					return String.Join("; ", vals);
				}

				return pv.TypedValue.DisplayValue;
			}

			return "";
		}

		/// <summary>
		/// Returns the lookup id of a Lookup propertyValue in the PropertyValue collection.
		/// </summary>
		/// <param name="prop">A reference to the propery whose lookup should be returned.</param>
		/// <returns>The id of the lookup if found, -1 otherwise.</returns>
		public int GetLookupID(object prop)
		{
			int i = -1;
			PropertyValue pv = GetProperty(prop);

			if (pv != null && !pv.Value.IsNULL() && (pv.Value.DataType == MFDataType.MFDatatypeLookup
				|| pv.Value.DataType == MFDataType.MFDatatypeMultiSelectLookup))
				i = pv.Value.GetValueAsLookups()[1].Item;

			return i;
		}

        /// <summary>
        /// Returns the property's value as a single Lookup.
        /// </summary>
        /// <param name="prop">A reference to a Lookup based property.</param>
        /// <returns>The Lookup value of the property. Empty Lookup object if property was not found.</returns>
        public Lookup GetLookup(object prop)
        {            
            PropertyValue pv = GetProperty(prop);

            if (pv != null)
                return pv.Value.GetValueAsLookup();

            return new Lookup();

        }


		/// <summary>
		/// Returns the property's value as a lookups collection.
		/// </summary>
		/// <param name="prop">A reference to a Lookup based property.</param>
		/// <returns>The lookups value of the property. Empty lookups object if property was not found.</returns>
		public Lookups GetLookups(object prop)
		{
			PropertyValue pv = GetProperty(prop);

			if (pv != null)
				return pv.Value.GetValueAsLookups();

			return new Lookups();

		}

		/// <summary>
		/// Attempts to retreive the specified PropertyValue in the PropertyValue collection.
		/// </summary>
		/// <param name="prop">A reference to the property.</param>
		/// <param name="propVal">The property value if found.</param>
		/// <returns>True if a property value was found, and the propVal value was set.</returns>
		public bool TryGetProperty(object prop, out PropertyValue propVal)
		{
			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);
			return this.Properties.TryGetProperty(id, out propVal);
		}


		/// <summary>
		/// Adds or updates the specified PropertyValue in the PropertyValue collection.
		/// </summary>
		/// <param name="prop">A reference to the property.</param>
		/// <param name="dataType">Datatype of the value/property.</param>
		/// <param name="value">The value to set for the property.</param>
		/// <returns>The ProperyValue added to the object.</returns>
		public PropertyValue SetProperty(object prop, MFDataType dataType, object value)
		{
			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);

			// Data type is unknown, we need to look it up.
			if (dataType == MFDataType.MFDatatypeUninitialized)
			{
				PropertyDef pd = this.Vault.PropertyDefOperations.GetPropertyDef(id);
				dataType = pd.DataType;
			}

			return this.Properties.SetProperty(id, dataType, value);
		}

		/// <summary>
		/// Adds or updates the specified PropertyValue in the PropertyValue collection.
		/// </summary>
		/// <param name="propVal">The property value to add to the object.</param>
		/// <returns>The actual ProperyValue added to the object.</returns>
		public PropertyValue SetProperty(PropertyValue propVal)
		{
			return this.Properties.SetProperty(propVal);
		}

		/// <summary>
		/// Removes the specified PropertyValue from the PropertyValue collection. 
		/// </summary>
		/// <param name="prop">A reference to the property to remove.</param>
		public void RemoveProperty(object prop)
		{
			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);
			this.Properties.RemoveProperty(id);
		}

		/// <summary>
		/// Sets the passed lookup item as the only value of  specified property in the PropertyValue collection. 
		/// </summary>
		/// <param name="prop">The propertyValue to set the lookup to.</param>
		/// <param name="item">A value reference to set the lookup value to.</param>
		/// <returns>The updated PropertyValue.</returns>
		public PropertyValue SetLookup(object prop, object item)
		{
			int propID = this.Vault.ResolveID(typeof(PropertyDef), prop);
			int itemID = this.Vault.ResolveItem(item).ID;
			return this.SetProperty(propID, MFDataType.MFDatatypeUninitialized, itemID);
		}

		/// <summary>
		/// Determines if the referenced item is present in the referenced property.
		/// </summary>
		/// <param name="prop">The propertyValue to check for the lookup.</param>
		/// <param name="item">A value reference to the lookup.</param>
		/// <returns>True if the item is present in the property's lookups.</returns>
		public bool HasLookup(object prop, object item)
		{
			Lookups lookups = this.GetLookups(prop);
			int itemID = this.Vault.ResolveItem(item).ID;
			return (lookups.GetLookupIndexByItem(itemID) != -1);
		}

		/// <summary>
		/// Adds a lookup item to the specified property in the PropertyValue collection. 
		/// Adding the property to the object if it doesn't already exist.
		/// </summary>
		/// <param name="prop">The propertyValue to add the lookup to.</param>
		/// <param name="item">A value reference to set the lookup value to.</param>
		/// <returns>True if the PropertyValue was updated.</returns>
		public bool AddLookup(object prop, object item)
		{
			int propID = this.Vault.ResolveID(typeof(PropertyDef), prop);
			int itemID = this.Vault.ResolveItem(item).ID;
			return this.Properties.AddLookup(propID, itemID);
		}

		public bool AddToLookups(object prop, Lookup item)
		{
			// resolve property
			int propID = this.Vault.ResolveID(typeof(PropertyDef), prop);

			// determine is single select or multi-select
			switch (this.Vault.PropertyDefOperations.GetPropertyDef(propID).DataType)
			{
				case MFDataType.MFDatatypeLookup:
					return this.Properties.AddLookup(propID, item.ToObjVer(this.Vault));
				case MFDataType.MFDatatypeMultiSelectLookup:
					{
						// create a PropertyValue
						PropertyValue propVal = new PropertyValue { PropertyDef = propID };

						Lookups lookups;
						lookups = this.HasValue(propID) ? this.GetLookups(propID) : new Lookups();
						lookups.Add(-1, item);

						propVal.Value.SetValueToMultiSelectLookup(lookups);
						this.Properties.SetProperty(propVal);
					}
					break;
			}
			return true;
		}

		public bool AddToLookups(object prop, Lookups items)
		{
			// resolve property
			int propID = this.Vault.ResolveID(typeof(PropertyDef), prop);
			
			// determine is single select or multi-select
			switch (this.Vault.PropertyDefOperations.GetPropertyDef(propID).DataType)
			{
				case MFDataType.MFDatatypeLookup:
					return false;
				case MFDataType.MFDatatypeMultiSelectLookup:
				{
					// create a PropertyValue
					PropertyValue propVal = new PropertyValue { PropertyDef = propID };
					propVal.Value.SetValueToMultiSelectLookup(items);
					this.Properties.SetProperty(propVal);
					return true;
				}
			}
			return false;;
		}

		/// <summary>
		/// Adds a lookup to the passed propertyvalue. Creating it if it doesn't already exist.
		/// </summary>
		/// <param name="prop">The propertyValue to add the lookup to</param>
		/// <param name="item">The objVer to set the lookup to</param>
		/// <param name="exactVersion">Indicates whether the exact version info should be used from the ObVer object.</param>
		/// <returns>True if the PropertyValue was updated.</returns>
		public bool AddLookup(object prop, ObjVer item, bool exactVersion = false)
		{
			int propID = this.Vault.ResolveID(typeof(PropertyDef), prop);
			return this.Properties.AddLookup(propID, item, exactVersion);
		}

		/// <summary>
		/// Removes a lookup item from the specified property in the PropertyValue collection.
		/// </summary>
		/// <param name="prop">A reference to a lookup based property.</param>
		/// <param name="item">The item to remove from the PropertyValue lookups.</param>
		/// <returns>True if the item was removed from the properties lookups. False if the property or item was not found.</returns>
		public bool RemoveLookup(object prop, object item)
		{
			int propID = this.Vault.ResolveID(typeof(PropertyDef), prop);
			int itemID = this.Vault.ResolveItem(item).ID;
			return this.Properties.RemoveLookup(propID, itemID);
		}
		
		/// <summary>
		/// Sets workflow and state values for the object.
		/// </summary>
		/// <param name="workflow">A reference to the workflow to set.</param>
		/// <param name="state">A reference to the state to set.</param>
		public void SetWorkflowState(object workflow = null, object state = null)
		{
			if (workflow == null && state == null)
				throw new ArgumentException("SetWorkflowState called with now workflow or state.");

			int wf = -1;
			int s = -1;

			// Resolve the state ID if a reference was passed.
			if (state != null)
				s = this.Vault.ResolveID(typeof(State), state);

			// Resolve the workflow ID if a reference was passed.
			if (workflow != null)
				wf = this.Vault.ResolveID(typeof(Workflow), workflow);

			// If no state reference was passed, resolve the state from the workflow
			if (state == null)
				s = MFUtils.GetFirstWorkflowState(this.Vault, wf);

			// If no workflow reference was passed, reolve the workflow from the state.
			if (workflow == null)
				wf = MFUtils.GetWorkflowIDByState(this.Vault, s);

			// Set the workflow and state properties.
			SetProperty(MFBuiltInPropertyDef.MFBuiltInPropertyDefWorkflow, MFDataType.MFDatatypeLookup, wf);
			SetProperty(MFBuiltInPropertyDef.MFBuiltInPropertyDefState, MFDataType.MFDatatypeLookup, s);
		}

		/// <summary>
		/// Returns the first reference found in the lookup based property specified.
		/// </summary>
		/// <param name="prop">A reference to the property that holds a certain object reference.</param>
		/// <returns>An ObjVerEx wrapped object reference resolved via the passed property.</returns>
		public ObjVerEx GetDirectReference(object prop)
		{
			// Delegate resolution to other method.
			List<ObjVerEx> list = GetDirectReferences(prop);

			// Return the first value, if anything was found.
			if (list.Count > 0)
				return list[0];

			// Nothing was found.
			return null;
		}

		/// <summary>
		/// Returns the all references found in the lookup based property specified.
		/// </summary>
		/// <param name="prop">A reference to the property that holds a certain object references.</param>
		/// <returns>An ObjVerEx wrapped list of objects resolved via the passed property.</returns>
		public List<ObjVerEx> GetDirectReferences(object prop)
		{
			// Get the property definition.
			PropertyDef pd = getValueListPropertyDef(prop);

			// Lookup the value list (object type) this property points to.
			ObjType ot = this.Vault.ObjectTypeOperations.GetObjectType(pd.ValueList);

			// Make sure the property points to real objects.
			if (!ot.RealObjectType)
				throw new ArgumentException("The property must point to objects. Property:" + prop.ToString());

			// create our return value
			List<ObjVerEx> list = new List<ObjVerEx>();

			// lookup the property value
			PropertyValue pv = GetProperty(prop);

			if (pv != null && !pv.Value.IsNULL())
			{
				// The property value exists and is not null.
				// Convert lookups to ObjVerExs based on type

				//if( pv.Value.DataType == MFDataType.MFDatatypeMultiSelectLookup )
				//{
				// Value is multi-select lookup, convert each lookup and add it to the list.
				foreach (Lookup l in pv.Value.GetValueAsLookups())
				{
					ObjVerEx obj = lookupToObjVerEx(ot.ID, l);

					// Add to list if not null.
					if (obj != null)
						list.Add(obj);
				}
				/*}
				else if( pv.Value.DataType == MFDataType.MFDatatypeLookup )
				{
					// Value is single-select lookup, convert lookup and add it to the list.
					ObjVerEx obj = lookupToObjVerEx( ot.ID, pv.Value.GetValueAsLookup() );

					// Add to list if not null.
					if( obj != null )
						list.Add( obj );
				}*/
			}

			// return our list of references;
			return list;

		}

		/// <summary>
		/// Returns the all the references pointing to the object 
		/// found in the lookup based property specified.
		/// </summary>
		/// <param name="prop">A reference to property which holds a reference to this object.</param>
		/// <returns>An ObjVerEx wrapped list of objects that refer to this object with the passed property.</returns>
		public List<ObjVerEx> GetIndirectReferences(object prop)
		{
			// TODO: Implement as a direct search.  This looks inefficient.


			// Get the property definition.
			PropertyDef propDef = getValueListPropertyDef(prop);

			// The returned list.
			List<ObjVerEx> returnedList = new List<ObjVerEx>();

			// Get all the object versions that point to the object.
			ObjectVersions versions = Vault.ObjectOperations
				.GetRelationships(this.ObjVer, MFRelationshipsMode.MFRelationshipsModeToThisObject);

			// Try to get property of each of the referer objects.
			foreach (ObjectVersion version in versions)
			{
				// Wrap as ObjVerEx.
				ObjVerEx referer = new ObjVerEx(Vault, version);

				// Try to get the specified property.
				PropertyValue foundProperty = referer.Properties.GetProperty(propDef.ID);
				if (foundProperty != null)
				{
					// The referer has the property, now we need to check whether
					// it is used to point to the object.
					foreach (Lookup lookup in foundProperty.TypedValue.GetValueAsLookups())
					{
						if (this.Info.ObjectGUID == lookup.ItemGUID)
						{
							// The referer points to the object with the property. Add to the list.
							returnedList.Add(referer);
							break;
						}
					}
				}
			}

			// Return the list.
			return returnedList;
		}

		/// <summary>
		/// Saves the specific property immediately.
		/// </summary>
		/// <param name="prop">A reference to the property</param>
		/// <param name="dataType">The datatype of the value.</param>
		/// <param name="value">The value to save for the property.</param>
		public void SaveProperty(object prop, MFDataType dataType, object value)
		{
			PropertyValues propVals = new PropertyValues();
			PropertyValue pv = SetProperty(prop, dataType, value);
			propVals.Add(-1, pv);
			update(this.Vault.ObjectPropertyOperations.SetProperties(this.ObjVer, propVals));
		}
		
		/// <summary>
		/// Saves all properties as they currently are set, or the ones passed.
		/// </summary>
		/// <param name="properties">
		/// Specific property values to set on the object (optional).
		/// If set, all updates to the internal propertyValues will be overwritten.
		/// If not set, updates to the internal propertyValues will be saved.
		/// </param>
		public void SaveProperties(PropertyValues properties = null)
		{
			// Branch by the property set features. If the property set specifies a object class, 
			// a complete property set must be passed.
			if (properties == null)
				properties = this.Properties;
			if( properties.IndexOf( ( int )MFBuiltInPropertyDef.MFBuiltInPropertyDefClass ) != -1 )
			{
				// Set the properties and update the internally stored state because the object version may be changed.
				update( this.Vault.ObjectPropertyOperations.SetAllProperties( this.ObjVer, true, properties ) );
			}
			else
			{
				// Update a single property only. Todo: should we have version check & retry if there is a 
				// newer version on the server.
				update( this.Vault.ObjectPropertyOperations.SetProperties( this.ObjVer, properties ) );
			}
		}

		/// <summary>
		/// Indicates whether the specified properties in the passed PropertyValue collection have values
		/// that match in this object.  If any of the property values cannot be resolved for either object
		/// there is never a match.
		/// </summary>
		/// <param name="propVals">A set of property values to compare.</param>
		/// <param name="props">References to properties whose valuse should be compared.</param>
		/// <returns>True if all the passed properties have values that match those in this object.</returns>
		public bool HasMatchingValues(PropertyValues propVals, params object[] props)
		{
			// HACK: unwrap params array, if runtime accidentally did.
			if (props.Length == 1 && props[0] is Array)
				props = (object[])props[0];

			// Loop over the properties that should match.
			foreach (object prop in props)
			{
				// Find this objects property.
				PropertyValue myProp = GetProperty(prop);

				// If this object's property is null, it can't be matched. Return false.
				if (myProp == null)
					return false;

				// Find the other object's property.
				PropertyValue otherProp = propVals.GetProperty(myProp.PropertyDef);

				// If other object's property is null, it can't be matched. Return false.
				if (otherProp == null)
					return false;

				// // If properties don't match. Return false.
				if (myProp.Value.CompareTo(otherProp.Value) != 0)
					return false;
			}

			// Everything has matched. Return true.
			return true;
		}

		/// <summary>
		/// Checks out the object. 
		/// </summary>
		public void CheckOut()
		{
			update(this.Vault.ObjectOperations.CheckOut(this.ObjID));
		}
		
		/// <summary>
		/// Checks in the object. 
		/// </summary>
		public void CheckIn(string comments = "", int user = -1)
		{
			if (comments != "")
			{
				// Set any check-in comments, if provided.
				SaveProperty(MFBuiltInPropertyDef.MFBuiltInPropertyDefVersionComment,
					MFDataType.MFDatatypeMultiLineText, comments);
			}

			if (user != -1)
			{
				// Set modified by information, if user provided.
				SetModifiedBy(user);

				if (this.Version <= 1)
				{
					// Also set created by value if this is the first version.
					SetCreatedBy(user);
				}
			}

			// Check-in the object and update the internals of the wrapper.
			update(this.Vault.ObjectOperations.CheckIn(this.ObjVer));
			props = null;
		}

		/// <summary>
		/// Asserts that the object is checked out. Throws an exception if it isn't. 
		/// </summary>
		public void AssertCheckedOut()
		{
			if (!this.Info.ObjectCheckedOut)
				throw new Exception("Object is checked out.");
		}
		
		/// <summary>
		/// Asserts that the object is checked out. Throws an exception if it isn't. 
		/// </summary>
		public void AssertCheckedOut(string customMessage)
		{
			if (!this.Info.ObjectCheckedOut)
				throw new Exception(customMessage);
		}

		/// <summary>
		/// Checks out the object if it isn't already.
		/// 
		/// Should be paired with the EndRequireCheckedOut() call when an operation
		/// requiring the object to be checked out is complete.  The return value of this
		/// method should be the first paramter of EndRequireCheckedOut().
		/// 
		/// </summary>
		/// <returns>True, if object was not previously checked out, but is now.</returns>
		public bool StartRequireCheckedOut()
		{
			if (!this.Info.ObjectCheckedOut)
			{
				this.CheckOut();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks in the object if it was previously checked out by StartRequireCheckedOut().
		/// </summary>
		/// <param name="start">The value obtained from StartRequireCheckedOut(). If true, the object will be checked in.</param>
		/// <param name="user">The modified by user to be set when checking in the object.</param>
		public void EndRequireCheckedOut(bool start, int user = -1)
		{
			if (readOnly)
				return;

			if (start && propertiesChanged)
			{
				//check in
				this.CheckIn("", user);
			}
			else if (propertiesChanged)
			{
				//save properties
				this.SaveProperties();
			}
		}

		/// <summary>
		/// Rolls back the object to a previous version, adding optional comment.
		/// </summary>
		/// <param name="version">The version to rollback to.</param>
		/// <param name="comment">The version comment for the rollback.</param>
		public void Rollback(int version, string comment = null)
		{
			update(this.Vault.ObjectOperations.Rollback(this.ObjID, version));
			props = null;

			// Add comment to version if passed.
			// We do it manually so it doesn't create an extra new version of the object.
			if (comment != null)
			{
				PropertyValue pv = new PropertyValue();
				pv.PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefVersionComment;
				pv.Value.SetValue(MFDataType.MFDatatypeMultiLineText, comment);
				this.Vault.ObjectPropertyOperations.SetProperty(this.ObjVer, pv);
			}
		}

		/// <summary>
		/// Deletes the underlying M-Files object.
		/// </summary>
		public void Delete()
		{
			this.Vault.ObjectOperations.RemoveObject(this.ObjID);
		}

		/// <summary>
		/// Destroys the underlying M-Files object.
		/// </summary>
		public void Destroy()
		{
			this.Vault.ObjectOperations.DestroyObject(this.ObjID, true, -1);
		}

		/// <summary>
		/// Forces the undo CheckOut operation.
		/// </summary>
		public void ForceUndoCheckout()
		{
			// Do the undo checkout and update the information.
			update( this.Vault.ObjectOperations.ForceUndoCheckout( this.ObjVer ) );

			// The properties have changed, empty them so that they will
			// be fetched again if needed.
			props = null;
		}

		/// <summary>
		/// Updates the last modified by property for the object.
		/// (Assumes the object is checked out)
		/// </summary>
		/// <param name="userID">The id of the user who will appear as the modifier.</param>
		public void SetModifiedBy(int userID)
		{
			PropertyValue pv = GetProperty(MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModifiedBy);
			pv.Value.SetValue(MFDataType.MFDatatypeLookup, userID);
			this.Vault.ObjectPropertyOperations.SetLastModificationInfoAdmin(this.ObjVer, true, pv.Value, false, pv.Value);
		}

		/// <summary>
		/// Updates the created by property for the object.
		/// (Assumes the object is checked out)
		/// </summary>
		/// <param name="userID">The id of the user who will appear as the creator.</param>
		public void SetCreatedBy(int userID)
		{
			PropertyValue pv = GetProperty(MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModifiedBy);
			pv.Value.SetValue(MFDataType.MFDatatypeLookup, userID);
			this.Vault.ObjectPropertyOperations.SetCreationInfoAdmin(this.ObjVer, true, pv.Value, false, pv.Value);
		}

		/// <summary>
		/// Indicates whether the passed user can delete this object version.
		/// </summary>
		/// <param name="userID">The user to check for.</param>
		/// <returns>True if the user can delete this version, otherwise.</returns>
		public bool CanDelete(int userID)
		{
			return this.Vault.SessionInfo.CheckObjectAccess(this.ACL, MFObjectAccess.MFObjectAccessDelete);
		}

		/// Indicates whether the passed user can edit this object version.
		/// </summary>
		/// <param name="userID">The user to check for.</param>
		/// <returns>True if the user can edit this version, otherwise.</returns>
		public bool CanEdit(int userID)
		{
			return this.Vault.SessionInfo.CheckObjectAccess(this.ACL, MFObjectAccess.MFObjectAccessEdit);
		}

		/// <summary>
		/// Replaces the object's existing files with the ones passed.
		/// </summary>
		/// <param name="files">The new files.</param>
		public void ReplaceFiles(SourceObjectFiles files)
		{
			if (this.Info.SingleFile)
			{
				// Single File Object

				// Throw an error if a single file wasn't passed.
				if (files.Count != 1)
					throw new InvalidOperationException(String.Format(QMS.Exception.WrongFileCount, files.Count));

				// Replace the existing file with the one passed.
				FileVer file = this.Info.Files[1].FileVer;
				this.Vault.ObjectFileOperations.UploadFile(file.ID, file.Version, files[1].SourceFilePath);

			}
			else
			{
				// Multi File Object

				// Remove existing files
				foreach (ObjectFile f in this.Info.Files)
					this.Vault.ObjectFileOperations.RemoveFile(this.ObjVer, f.FileVer);

				// Add passed files
				foreach (SourceObjectFile sf in files)
					this.Vault.ObjectFileOperations.AddFile(this.ObjVer, sf.Title, sf.Extension, sf.SourceFilePath);
			}
		}

		/// <summary>
		/// Returns a string with M-Files placeholders replaced with values. 
		/// </summary>
		/// <param name="format">A string containing placeholders that should be replaced.</param>
		/// <param name="hideMissingValues">
		/// Indicates whether placeholders should be left as they are if the object doesn't have a
		/// property referenced by a placeholder, instead of being replaced by an empty string.
		/// If set to false, format string can be passed to multiple objects.
		/// </param>
		/// <returns></returns>
		public string ExpandPlaceholderText(string format, bool hideMissingValues = true)
		{
			// Replace static placeholders with their values.
			string expandedText = format
				.Replace("%OBJID%", this.ID.ToString())
				.Replace("%OBJVER%", this.Version.ToString());


			// Loop over any property placeholders found.
			MatchCollection matches = Regex.Matches(format,
				@"%((?:PROPERTY|OBJTYPE)_\d+(?:.(?:PROPERTY|OBJTYPE)_\d+)*)%", RegexOptions.IgnoreCase);
			foreach (Match match in matches)
			{
				// Parse the placeholder into indirection levels
				string[] levels = match.Groups[1].Value.Split('.');

				// Reset initial resolution object for each match.
				List<ObjVerEx> objects = new List<ObjVerEx> { this };
				Lookups values = new Lookups();

				// Loop over indirection levels
				for (int i = 0; i < levels.Length; i++)
				{
					// Parse the level string
					string[] parts = levels[i].Split('_');
					string type = parts[0];
					int id = Int32.Parse(parts[1]);

					// Convert object type id to property def id.
					if (type.ToUpper() == "OBJTYPE")
					{
						ObjType objType = this.Vault.ObjectTypeOperations.GetObjectType(id);
						id = objType.DefaultPropertyDef;
					}

					if (i == levels.Length - 1)
					{
						// Last level of indirection, resolve the actual values from found objects

						// loop over objects resolved through indirection
						foreach (ObjVerEx obj in objects)
						{
							// Add a found value as a lookup.
							PropertyValue pv = obj.GetProperty(id);
							if (pv != null && !pv.Value.IsNULL())
							{
								Lookup l = new Lookup();
								l.DisplayValue = pv.Value.DisplayValue;
								values.Add(-1, l);
							}
						}
					}
					else
					{
						// if an indirection level doesn't point to a real object based property
						// nothing will be resolved, just quit.
						PropertyDef def = this.Vault.PropertyDefOperations.GetPropertyDef(id);
						if (!def.BasedOnValueList)
							break;
						if (!this.Vault.ObjectTypeOperations.GetObjectType(def.ValueList).RealObjectType)
							break;

						// resolve next level of indirection from the current indirection objects
						List<ObjVerEx> nextObjects = new List<ObjVerEx>();
						foreach (ObjVerEx obj in objects)
							nextObjects.AddRange(obj.GetDirectReferences(id));

						objects = nextObjects;
					}
				}

				if (hideMissingValues || values.Count > 0)
				{
					// Referenced property values were found, or we should hide the placeholder anyways. 

					// Resolve the value.
					TypedValue tv = new TypedValue();
					tv.SetValueToMultiSelectLookup(values);

					//Replace the placeholder with the resolved value.
					expandedText = expandedText.Replace(match.Groups[0].Value, tv.DisplayValue);
				}
			}

			return expandedText;

		}


		/// <summary>
		/// Returns a string representation of the object version.
		/// </summary>
		/// <returns>String representation of the object "(ObjType-ID-Version)"</returns>
		public override string ToString()
		{
			return String.Format("({0}-{1}-{2})", this.Type, this.ID, this.Version);
		}

		/// <summary>
		/// Returns a string representation of the object version.
		/// </summary>
		/// <param name="humanReadable">If true, it will add the object title to the ouptut.</param>
		/// <returns>String representation of the object "(ObjType-ID-Version)"</returns>
		public string ToString(bool humanReadable)
		{
			string idString = this.ToString();

			if (humanReadable)
				return idString + ": " + this.Title;
			else
				return idString;
		}


		/// <summary>
		/// Overrides hashcode method for efficient use in HashSets.
		/// </summary>
		/// <returns>The id of the object.</returns>
		public override int GetHashCode()
		{
			return this.ID;
		}

		/// <summary>
		/// Overrides generic equals function.
		/// </summary>
		/// <returns>Return true if object is an ObjVerEx and the type, id, and version match.</returns>
		public override bool Equals(object obj)
		{
			bool result = false;

			if (obj is ObjVerEx)
				result = Equals((ObjVerEx)obj);

			return result;
		}

		/// <summary>
		/// Overrides type specific equals function.
		/// </summary>
		/// <returns>Return true if object type, id, and version match.</returns>
		public bool Equals(ObjVerEx other)
		{
			bool result = false;

			if (other != null)
				result = (this.ID == other.ID
					&& this.Type == other.Type
					&& this.Version == other.Version);

			return result;
		}

		/// <summary>
		/// Updates the ObjVerEx to the Latest Version on the Server
		/// </summary>
		public void Refresh()
		{
			this.update(this.Vault.ObjectOperations.GetLatestObjectVersionAndProperties(this.ObjID, true, true));
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Returns an ObjVerEx object pointing to the latest version of the M-Files object.
		/// </summary>
		/// <param name="vault">The vault in which the object exists.</param>
		/// <param name="objID">The ObjID of the object.</param>
		/// <returns>The new ObjVerEx object.</returns>
		public static ObjVerEx Latest(Vault vault, ObjID objID)
		{
			ObjVer objVer = vault.ObjectOperations.GetLatestObjVer(objID, true, true);
			return new ObjVerEx(vault, objVer);
		}

		/// <summary>
		/// Loads an ObjVerEx object based on a ObjID or ObjVer string representation
		/// </summary>
		/// <param name="vault">The vault in which the object exists.</param>
		/// <param name="s">The string to be parsed.</param>
		/// <returns>The new ObjVerEx object.</returns>
		public static ObjVerEx Parse(Vault vault, string s)
		{

			// Try and parse the passed string.
			if (MFUtils.IsObjVerString(s))
			{
				// String is ObjVer format. Parse it to an ObjVer.
				ObjVer objVer = MFUtils.ParseObjVerString(s);

				// Check if we still need to look up the correct version.
				if (objVer.Version == -1)
					return ObjVerEx.Latest(vault, objVer.ObjID);
				else
					return new ObjVerEx(vault, objVer);
			}
			else if (MFUtils.IsObjIDString(s))
			{
				// String is ObjID format. Parse it to an ObjID.
				ObjID objID = MFUtils.ParseObjIDString(s);
				return ObjVerEx.Latest(vault, objID);
			}
			else
			{
				// Unrecognized string. Throw exception.
				throw new ArgumentException("The passed string is not valid.");
			}

		}


		/// <summary>
		/// Tries to loads an ObjVerEx object based on a ObjID or ObjVer string representation
		/// </summary>
		/// <param name="vault">The vault in which the object exists.</param>
		/// <param name="s">The string to be parsed.</param>
		/// <param name="objVerEx">Set to the ObjVerEx object if successful.</param>
		/// <returns>True if successful</returns>
		public static bool TryParse(Vault vault, string s, out ObjVerEx objVerEx)
		{
			objVerEx = null;

			try
			{
				objVerEx = ObjVerEx.Parse(vault, s);
				return true;
			}
			catch (Exception) { }

			return false;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates some cached data within this object.
		/// </summary>
		/// <param name="ovap">New object version details.</param>
		private void update(ObjectVersionAndProperties ovap)
		{
			this.ObjVer = ovap.ObjVer;
			info = ovap.VersionData;
			props = ovap.Properties;
		}

		/// <summary>
		/// Updates some cached data within this object.
		/// </summary>
		/// <param name="versionInfo">New object version details.</param>
		private void update(ObjectVersion versionInfo)
		{
			info = versionInfo;
			this.ObjVer = info.ObjVer;
		}

		/// <summary>
		/// Updates the objVerEx to represent the latest version of this object.
		/// </summary>
		private void loadLatestObjVer()
		{
			this.ObjVer = this.Vault.ObjectOperations.GetLatestObjVer(this.ObjVer.ObjID, true, true);
		}

		/// <summary>
		/// Converts a lookup to an ObjVerEx
		/// </summary>
		/// <param name="objType">The object type of the lookup items.</param>
		/// <param name="lookup">The lookup to convert.</param>
		/// <returns>An ObjVerEx object representing the lookup item.</returns>
		private ObjVerEx lookupToObjVerEx(int objType, Lookup lookup)
		{
			try
			{
				return new ObjVerEx(this.Vault, objType, lookup.Item, lookup.Version);
			}
			catch (System.Runtime.InteropServices.COMException exception)
			{
				// Check if we have a not found error message.
				if (!MFUtils.IsMFilesNotFoundError(exception))
				{
					// It is not "not found", rethrow.
					throw exception;
				}

				// The selected lookup was not found, so omit it from the listing.
				return null;
			}
		}

		/// <summary>
		/// Resolves hidden text in a lookup item.
		/// </summary>
		/// <param name="propDef">The property def in which the lookup is stored.</param>
		/// <param name="lookup">The lookup to resolve the text for.</param>
		/// <returns>The text value of the lookup.</returns>
		private string getLookupText(PropertyDef propDef, Lookup lookup)
		{
			if (!lookup.Hidden)
				return lookup.DisplayValue;

			return this.Vault.ValueListItemOperations.GetValueListItemByID(propDef.ValueList, lookup.Item).Name;
		}

		/// <summary>
		/// Returns a PropertyDef that matches the given object. Throws ArgumentExceptions if property id was not resolved
		/// or the property definition was not based on a value list.
		/// </summary>
		/// <param name="prop">Resolvable object</param>
		/// <returns>PropertyDef</returns>
		private PropertyDef getValueListPropertyDef(object prop)
		{
			// Resolve the property id.
			int id = this.Vault.ResolveID(typeof(PropertyDef), prop);

			// Make sure the property id was resolved.
			if (id == -1)
				throw new ArgumentException("Property does not exist in vault. Property:" + prop.ToString());

			// Lookup the PropertyDef.
			PropertyDef pd = this.Vault.PropertyDefOperations.GetPropertyDef(id);

			// Make sure the PropertyDef is based on a value list.
			if (!pd.BasedOnValueList)
				throw new ArgumentException("Property has wrong datatype. Property:" + prop.ToString());

			return pd;
		}

		#endregion

	}
}

