using System;
using System.Collections.Generic;
using NAMESPACE;
using MFilesAPI;

namespace NAMESPACE
{
	/// <summary>
	/// Convenience methods for Vault objects.
	/// </summary>
	public static class ExtensionMethods
	{

		// Declares our id resolution functions by type
		private static Dictionary<Type, VaultElementFinder> lookupIDDelegatesByType = new Dictionary<Type, VaultElementFinder> {

			{ typeof(PropertyDef), 
				new VaultElementFinder 
				{
					FromAlias = (v,s) => { return v.PropertyDefOperations.GetPropertyDefIDByAlias(s); },
					FromGuid =  (v,s) => { return v.PropertyDefOperations.GetPropertyDefIDByGUID(s); },
					FromID = (v,i) => { return v.PropertyDefOperations.GetPropertyDef(i).ID; }
				}
			},

			{ typeof(ObjType), 
				new VaultElementFinder 
				{
					FromAlias = (v,s) => { return v.ObjectTypeOperations.GetObjectTypeIDByAlias(s); },
					FromGuid =  (v,s) => { return v.ObjectTypeOperations.GetObjectTypeIDByGUID(s); },
					FromID = (v,i) => { return v.ObjectTypeOperations.GetObjectType(i).ID; }
				}
			},

			{ typeof(ObjectClass), 
				new VaultElementFinder 
				{
					FromAlias = (v,s) => { return v.ClassOperations.GetObjectClassIDByAlias(s); },
					FromGuid =  (v,s) => { return v.ClassOperations.GetObjectClassIDByGUID(s); },
					FromID =  (v,i) => { return v.ClassOperations.GetObjectClass(i).ID; },
				}
			},

			{ typeof(Workflow), 
				new VaultElementFinder 
				{
					FromAlias = (v,s) => { return v.WorkflowOperations.GetWorkflowIDByAlias(s); },
					FromGuid =  (v,s) => { return v.WorkflowOperations.GetWorkflowIDByGUID(s); },
					FromID =  (v,i) => { return v.WorkflowOperations.GetWorkflowForClient(i).ID; }
				}
			},

			{ typeof(State), 
				new VaultElementFinder 
				{
					FromAlias = (v,s) => { return v.WorkflowOperations.GetWorkflowStateIDByAlias(s); },
					FromGuid =  (v,s) => { return v.WorkflowOperations.GetWorkflowStateIDByGUID(s); }
				}
			},

			{ typeof( NamedACL),
				new VaultElementFinder
				{
					FromAlias = (v,s) => { return v.NamedACLOperations.GetNamedACLIDByAlias(s); },
					FromGuid =  (v,s) => { return v.NamedACLOperations.GetNamedACLIDByGUID(s); },
					FromID =  (v,i) => { return v.NamedACLOperations.GetNamedACL(i).ID; }
				}
			},

			{ typeof(ObjID),
				new VaultElementFinder
				{
					FromGuid =  (v,s) => { return v.ObjectOperations.GetObjIDByGUID( s ).ID; }
				}
			}

		};
		
	    /// <summary>
		/// Resolves the id of a vault element by reference.
		/// </summary>
		/// <typeparam name="T">The type of element the reference refers to.</typeparam>
		/// <param name="v">The vault in which the element is defined.</param>
		/// <param name="Identifier">
		///	 Identifier can be:
		///		- int (ID of the element)
		///		- string (GUID, ID or Alias of the element)
		///		- GUID (GUID of the element)
		///		- enum (ID of the element)
		/// </param>
		/// <returns>The id of the element, the int value of the reference, or -1 if reference not resolved.</returns>
		public static int ResolveID(this Vault vault, Type type, object reference)
		{

			// Make sure a valid Identifier has been specified
			if (reference == null)
				throw new ArgumentNullException("reference");

			// Get a handle on this Type's lookup methods
			VaultElementFinder typedFinder = lookupIDDelegatesByType[type];

			int i;

			if (reference is int)
			{
				// Reference is an integer, assume it is an id and return it.
				return (int)reference;
			}
			else if (reference is MFIdentifier)
			{
				// Reference is an MFIdentifier, resovle the ID and return it.
				return (reference as MFIdentifier).Resolve(vault, type);
			}
			else if (reference is Guid)
			{
				// The reference is a Guid, lookup the ID with the type specific FromGuid delegate.
				return typedFinder.FromGuid.Invoke(vault, ((Guid)reference).ToString("B"));
			}
			else if (reference is string)
			{
				// Reference is a string

				// Try to parse and handler identifier
				if (Int32.TryParse((string)reference, out i))
				{
					// Identifier can be converted to int. Return the converted value.
					return i;
				}
				else if (MFUtils.IsValidGuid((string)reference))
				{
					// Identifier is formatted as a GUID, lookup ID with it.
					return typedFinder.FromGuid.Invoke(vault, (string)reference);
				}
				else
				{
					// Assume the Identifier is an Alias and lookup ID with it.
					return typedFinder.FromAlias.Invoke(vault, (string)reference);
				}
			}
			else
			{
				// Reference is not a string, try to convert it to our id.
				try
				{
					i = Convert.ToInt32(reference);
					return i;
				}
				catch (Exception) { }
			}

			// We couldn't resolve anything. Throw an exception.
			throw new ArgumentException(String.Format(QMS.Exception.IdentifierNotRecognized, reference.ToString()));

		}

		/// <summary>
		/// Resolves the object/ValueListItem id for the passed reference
		/// </summary>
		/// <param name="vault"></param>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static ObjID ResolveItem(this Vault vault, object reference)
		{

			// Make sure a valid reference was passed.
			if (reference == null)
				throw new ArgumentNullException("reference");


			// Try to lookup the ObjID.
			if (reference is ObjID)
			{
				// Reference is already an ObjID, return it.
				return (ObjID)reference;
			}
			else if (reference is ObjVer)
			{
				// Reference is an ObjVer, return the ObjID portion.
				return ((ObjVer)reference).ObjID;
			}
			else if (reference is ObjVerEx)
			{
				// Reference is an ObjVer, return the ObjID portion.
				return ((ObjVerEx)reference).ObjID;
			}
			else if (reference is int)
			{
				// Reference is an int, return an ObjID with only the ID part set.
				ObjID objID = new ObjID();
				objID.ID = (int)reference;
				return objID;
			}
			else if (reference is string)
			{
				// Reference is a string. Try to parse it.

				// Try to parse as id.
				int i;
				if (Int32.TryParse((string)reference, out i))
				{
					// Reference is an int in string form, return an ObjID with only the ID part set.
					ObjID objID = new ObjID();
					objID.ID = (int)i;
					return objID;
				}
				else if (MFUtils.IsObjIDString((string)reference))
				{
					// Identifier is an ObjID string. Parse It.
					return MFUtils.ParseObjIDString((string)reference);
				}
				else if (MFUtils.IsValidGuid((string)reference))
				{
					// Identifier is a GUID. Lookup the object with it.
					return vault.ObjectOperations.GetObjIDByGUID((string)reference);
				}
			}

			// Unrecognized 
			throw new InvalidOperationException("The object/item reference is not valid. " + reference.ToString());

		}

		// a delegate to lookup an id with an alias or guid
		private delegate int LookupID(Vault v, string s);

		// a delegate to lookup an id with an id
		private delegate int LookupIDID(Vault v, int i);

		// An object that holds specific lookup functions for a given type
		private class VaultElementFinder
		{
			/// <summary>
			/// A delegate that will lookup an element by alias.
			/// </summary>
			public LookupID FromAlias { get; set; }

			/// <summary>
			/// A delegate that will lookup an element by GUID.
			/// </summary>
			public LookupID FromGuid { get; set; }

			/// <summary>
			/// A delegate that will lookup/verify an element by ID.
			/// </summary>
			public LookupIDID FromID { get; set; }
		}

	}


}
