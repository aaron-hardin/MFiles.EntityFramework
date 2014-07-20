using System;
using System.Text.RegularExpressions;
using MFilesAPI;

namespace NAMESPACE
{

	/// <summary>
	/// Format strings used by MFUtils.
	/// </summary>
	static class MFUtilsConstants
	{
		//String representation formats.
		static internal readonly string ObjIDFormat = "({0}-{1})";
		static internal readonly string ObjIDRegEx = @"^\(([0-9]+)-([0-9]+)\)$";
		static internal readonly string ObjVerFormat = "({0}-{1}-{2})";
		static internal readonly string ObjVerRegEx = @"^\(([0-9]+)-([0-9]+)-([0-9]+)\)$";

		// Exceptions

		/// <summary>
		/// {0} = Object type we are trying to parse the string into.
		/// {1} = The string representation of the object to be parsed.
		/// </summary>
		static internal readonly string ParseError = "The {0} string '{1}' is not valid.";

		/// <summary>
		/// {0} = Workflow ID
		/// {1} = Workflow Name
		/// </summary>
		static internal readonly string NoWorkflowStates = "The workflow \"{1}\" ({0}) has no states.";
	}

	/// <summary>
	/// Provides some common utility methods for M-Files operations.
	/// </summary>
	public static class MFUtils
	{

		/// <summary>
		/// List of properties included automatically as associated property defs in all classes.
		/// </summary>
		public static readonly int[] DefaultClassPropertyDefs = new int[]
		{
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreated,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreatedBy,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModified,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModifiedBy,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefStatusChanged,
			89, // Simple signature property
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefSingleFileObject,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefClassGroups,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefSizeOnServerThisVersion,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefSizeOnServerAllVersions,
			(int)MFBuiltInPropertyDef.MFBuiltInPropertyDefMarkedForArchiving
		};

		/// <summary>
		/// Indicates whether the passed string is a vaild M-Files formatted GUID.
		/// 
		/// {00000000-0000-0000-0000-000000000000}
		/// 
		/// </summary>
		/// <param name="guid">A guid string.</param>
		/// <returns>True if the string has the correct format, false otherwise.</returns>
		public static bool IsValidGuid(string guid)
		{
			// Return true if the Identifier can be parsed as a GUID with the standard M-Files GUID format.
			// "B" is the format with the curly braces.
			Guid g;
			if (Guid.TryParseExact(guid, "B", out g))
				return true;
			else
				return false;
		}

		/// <summary>
		/// Converts an ObjID into human readable, and code-parseable string.
		/// </summary>
		/// <param name="objID">The source ObjID object.</param>
		/// <returns>A string represenation of the ObjID.</returns>
		public static string ObjIDString(ObjID objID)
		{
			return String.Format(MFUtilsConstants.ObjIDFormat, objID.Type, objID.ID);
		}

		/// <summary>
		/// Indicates if the passed string matches a valid ObjID format.
		/// </summary>
		/// <param name="objIDString">A string representation of an ObjID</param>
		/// <returns>True if the string has the correct format, false otherwise.</returns>
		public static bool IsObjIDString(string objIDString)
		{
			return Regex.IsMatch(objIDString.Trim(), MFUtilsConstants.ObjIDRegEx);
		}

		/// <summary>
		/// Parses a string into an ObjID object.
		/// </summary>
		/// <param name="objIDString">A string representation of an ObjID.</param>
		/// <returns>The corresponding ObjID object</returns>
		public static ObjID ParseObjIDString(string objIDString)
		{
			// Throw an exception if the string does not have the correct format.
			if (!MFUtils.IsObjIDString(objIDString))
				throw new ArgumentException(String.Format(MFUtilsConstants.ParseError, "ObjID", objIDString));

			// Apply the ObjID regulare expression to get the values.
			Match match = Regex.Match(objIDString.Trim(), MFUtilsConstants.ObjIDRegEx);

			// Create the ObjID with the values extracted by the regex.
			ObjID objID = new ObjID();
			objID.Type = Convert.ToInt16(match.Groups[1].Value);
			objID.ID = Convert.ToInt16(match.Groups[2].Value);

			return objID;
		}

		/// <summary>
		/// Converts an ObjVer into human readable, and code-parseable string.
		/// </summary>
		/// <param name="objID">The source ObjVer object.</param>
		/// <returns>A string represenation of the ObjVer.</returns>
		public static string ObjVerString(ObjVer objVer)
		{
			return String.Format(MFUtilsConstants.ObjVerFormat, objVer.Type, objVer.ID, objVer.Version);
		}

		/// <summary>
		/// Indicates if the passed string matches a valid ObjVer format.
		/// </summary>
		/// <param name="objVerString">A string representation of an ObjVer.</param>
		/// <returns>True if the string has the correct format, false otherwise.</returns>
		public static bool IsObjVerString(string objVerString)
		{
			return Regex.IsMatch(objVerString.Trim(), MFUtilsConstants.ObjVerRegEx);
		}

		/// <summary>
		/// Parses a string into an ObjVer object.
		/// </summary>
		/// <param name="objVerString">A string representation of an ObjVer.</param>
		/// <returns>The corresponding ObjVer object.</returns>
		public static ObjVer ParseObjVerString(string objVerString)
		{
			// Throw an exception if the string does not have the correct format.
			if (!MFUtils.IsObjVerString(objVerString))
				throw new ArgumentException(String.Format(MFUtilsConstants.ParseError, "ObjVer", objVerString));

			// Apply the ObjVer regulare expression to get the values.
			Match match = Regex.Match(objVerString.Trim(), MFUtilsConstants.ObjVerRegEx);

			// Create the ObjVer with the values extracted by the regex.
			ObjVer objVer = new ObjVer();
			objVer.Type = Convert.ToInt16(match.Groups[1].Value);
			objVer.ID = Convert.ToInt16(match.Groups[2].Value);
			objVer.Version = Convert.ToInt16(match.Groups[3].Value);

			return objVer;
		}


		/// <summary>
		/// Finds a state's workflow.
		/// </summary>
		/// <param name="vault">The vault in which the state exists.</param>
		/// <param name="state">A reference to the state.</param>
		/// <returns>The id of the workflow which the passed state belongs to.</returns>
		public static int GetWorkflowIDByState(Vault vault, MFIdentifier state)
		{
			// Resolve the state as a ValueListItem.
			state.Resolve(vault, typeof(State));
			ValueListItem i = vault.ValueListItemOperations.GetValueListItemByID(
				(int)MFBuiltInValueList.MFBuiltInValueListStates, state);

			// Return the ValueListItem's (State's) Owner (Workflow) ID
			return i.OwnerID;
		}


		/// <summary>
		/// Gets the first workflow state defined by a workflow.
		/// </summary>
		/// <param name="vault">The vault in which the workflow resides.</param>
		/// <param name="workflow">A reference to the workflow</param>
		/// <returns>The ID of the first workflow state defined in the passed workflow.</returns>
		public static int GetFirstWorkflowState(Vault vault, MFIdentifier workflow)
		{
			// Resolve workflowAdmin defintion
			WorkflowAdmin wf = vault.WorkflowOperations.GetWorkflowAdmin(workflow);

			// Ensure the workflow has at least 1 state.
			if (wf.States.Count < 1)
				throw new Exception(String.Format(MFUtilsConstants.NoWorkflowStates, wf.Workflow.ID, wf.Workflow.Name));

			// Return the first state's id.
			return wf.States[1].ID;
		}

		/// <summary>
		/// Retrieves the StateAdmin object for a state.
		/// </summary>
		/// <param name="vault">The vault in which the state resides</param>
		/// <param name="state">A reference to the state.</param>
		/// <returns>The StateAdmin object of the passed state.</returns>
		public static StateAdmin GetStateAdmin(Vault vault, MFIdentifier state)
		{
			// Resolve state id.
			state.Resolve(vault, typeof(State));

			// Resolve workflow id,
			int workflowID = MFUtils.GetWorkflowIDByState(vault, state);

			// Resolve workflowAdmin
			WorkflowAdmin wfAdmin = vault.WorkflowOperations.GetWorkflowAdmin(workflowID);

			// Find stateAdmin object under workflow
			foreach (StateAdmin s in wfAdmin.States)
			{
				if (s.ID == state.ID)
					return s;
			}

			// State wasn't found (shouldn't be possible);
			return null;
		}

		/// <summary>
		/// Retrieves the Property value of a State action for specific property if it 
		/// exists. Returns null if property or action set properties not found.
		/// </summary>
		/// <param name="state">StateAdmin object of the specific state</param>
		/// <param name="propID">ID of the property definition as an integer</param>
		/// <returns>TypedValue object of the value. If nothing found, returns null.</returns>
		public static TypedValue GetStateActionSetPropertyValue(StateAdmin state, int propID)
		{
			// Check if the state has set properties action in it.
			if (state.ActionSetProperties)
			{
				// Loop through all the properties that are set.
				foreach (DefaultProperty property in state.ActionSetPropertiesDefinition.Properties)
				{
					// Check if the property definition ID is the same.
					if (property.PropertyDefID == propID)
					{
						// As the property definition ID is the same, we
						// return the value.
						return property.DataFixedValueValue;
					}
				}
			}

			// If nothing was found, return null.
			return null;
		}


		/// <summary>
		/// Sets a value to the named value storage.
		/// </summary>
		/// <param name="vault">Target vault.</param>
		/// <param name="confValue">Named value type.</param>
		/// <param name="confNamespace">Named value namespace.</param>
		/// <param name="name">Value key.</param>
		/// <param name="value">Value content.</param>
		public static void SetTransactionVariable(Vault vault, MFNamedValueType confValue,
			string confNamespace, string name, object value)
		{

			// Create a namedvalues instance from the key/value pair and set it to the named
			// value storage.
			NamedValues values = new NamedValues();
			values[name] = value;
			vault.NamedValueStorageOperations.SetNamedValues(confValue, confNamespace, values);
		}

		/// <summary>
		/// Gets a specific value from the named value storage.
		/// </summary>
		/// <param name="vault">Target vault.</param>
		/// <param name="confValue">Named value type.</param>
		/// <param name="confNamespace">Named value namespace.</param>
		/// <param name="name">Value key.</param>
		/// <param name="value">Value content.</param>
		/// <returns>Indication if value was found.</returns>
		public static bool TryGetTransactionVariable(Vault vault, MFNamedValueType confValue,
			string confNamespace, string name, out object value)
		{
			// Get values.
			NamedValues values = vault.NamedValueStorageOperations.GetNamedValues(confValue, confNamespace);

			// Check if value name is found.
			if (values.Contains(name))
			{
				// Return value and true.
				value = values[name];
				return true;
			}
			else
			{
				// Return null and false.
				value = null;
				return false;
			}
		}

		/// <summary>
		/// Gets all values from a specific named value storage namespace.
		/// </summary>
		/// <param name="vault">Target vault.</param>
		/// <param name="confValue">Named value type.</param>
		/// <param name="confNamespace">Named value namespace.</param>
		/// <returns>Indication if value was found.</returns>
		public static NamedValues GetTransactionVariables(Vault vault, MFNamedValueType confValue,
			string confNamespace)
		{
			// Return found values.
			return vault.NamedValueStorageOperations.GetNamedValues(confValue, confNamespace);
		}

		/// <summary>
		/// Removes a key from the named value storage.
		/// </summary>
		/// <param name="vault">Target vault.</param>
		/// <param name="confValue">Named value type.</param>
		/// <param name="confNamespace">Named value namespace.</param>
		/// <param name="name">Value key.</param>
		public static void ClearTransactionVariable(Vault vault, MFNamedValueType confValue,
			string confNamespace, string name)
		{
			// Strings instance for storing the key.
			Strings names = new Strings();
			names.Add(-1, name);

			// Remove key from the named value storage.
			vault.NamedValueStorageOperations.RemoveNamedValues(confValue, confNamespace, names);
		}

		/// <summary>
		/// Checks if the COMException is a 'not found' error.
		/// </summary>
		/// <param name="exception">The COMException that should be checked.</param>
		/// <returns></returns>
		public static bool IsMFilesNotFoundError(System.Runtime.InteropServices.COMException exception)
		{
			// Check if the not found error code can be found from the error message.
			if ( exception.Message.IndexOf( "(0x8004000B)" ) != -1 ||
				exception.Message.IndexOf( "(0x800408A4)" ) != -1 )
				return true;

			// The error was other than the 'not found'.
			return false;
		}

		/// <summary>
		/// Checks if the Exception is a M-Files 'already exists' error.
		/// </summary>
		/// <param name="exception">The COMException that should be checked.</param>
		/// <returns></returns>
		public static bool IsMFilesAlreadyExistsError( Exception exception )
		{
			// Check if the error code can be found from the error message.
			if( exception is System.Runtime.InteropServices.COMException && exception.Message.IndexOf( "(0x80040031)" ) != -1 )
				return true;
			return false;
		}

		/// <summary>
		/// Checks if the COMException is an 'Object Locked' error.
		/// </summary>
		/// <param name="exception">The COMException that should be checked.</param>
		/// <returns></returns>
		public static bool IsMFilesObjectLockedError(System.Runtime.InteropServices.COMException exception)
		{
			// Check if the not found error code can be found from the error message.
			if (exception.Message.IndexOf("(0x80040041)") != -1)
				return true;

			// The error was other than the 'not found'.
			return false;
		}

		/// <summary>
		/// Checks if the COMException is an 'There is a newer version of this object on the server' error.
		/// </summary>
		/// <param name="exception">The COMException that should be checked.</param>
		/// <returns></returns>
		public static bool IsMFilesNewerVersionError( System.Runtime.InteropServices.COMException exception )
		{
			// Check if the expected error code can be found from the error message.
			if( exception.Message.IndexOf( "(0x800400BB)" ) != -1 )
				return true;

			// The error was other than the expected one.
			return false;
		}
		

	}
}
