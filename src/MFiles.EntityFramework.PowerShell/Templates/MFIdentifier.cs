using System;
using MFilesAPI;
using System.Runtime.Serialization;

namespace NAMESPACE
{
	/// <summary>
	/// Identification object type, that can be set to string and int.
	/// Implicit conversion to integer, explicit to alias string.
	/// </summary>
	[DataContract( Namespace = "", Name = "MetadataIdentity" )]
	public class MFIdentifier
	{
		/// <summary>
		/// Alias string.
		/// Setting this will clear the resolved ID value.
		/// </summary>
		[DataMember]
		public string Alias
		{
			get { return this.alias; }
			set
			{
				// Set alias to empty string or the assigned value.
				if( value == null )
					this.alias = "";
				else
					this.alias = value;

				// Clear the ID, as it might not match the new alias.
				this.ID = UnresolvedID;
				this.resolvedOnce = false;
			}
		}

		/// <summary>
		/// ID integer.
		/// </summary>
		public int ID { get; set; }

		/// <summary>
		/// GUID if original string value was valid GUID, else empty string.
		/// </summary>
		/// <remarks>Internally GUID is not any different from Alias.</remarks>
		[IgnoreDataMember]
		public string GUID
		{
			get
			{
				if( MFUtils.IsValidGuid( this.alias ) )
					return this.alias;
				return "";
			}
			set
			{
				if( MFUtils.IsValidGuid( value ) )
					throw new ApplicationException( "'" + value + "' is not valid GUID." );
				this.Alias = value;
			}
		}


		/// <summary>
		/// Value of unresolved ID used as "error-value"
		/// </summary>
		protected const int UnresolvedID = -1;

		/// <summary>
		/// Internal field of Alias string.
		/// </summary>
		[IgnoreDataMember]
		private string alias;

		/// <summary>
		/// Internal version of original ID value, not the resolved one.
		/// </summary>
		[IgnoreDataMember]
		private int originalID;

		#region Serialization helpers

		/// <summary>
		/// Method deciding should we include Alias into serialization output.
		/// Yes when we have an alias.
		/// </summary>
		public bool ShouldSerializeAlias()
		{
			return ( Alias.Length > 0 );
		}
		/// <summary>
		/// Method deciding should we include ID into serialization output.
		/// Yes only when we dont have an alias.
		/// </summary>
		public bool ShouldSerializeID()
		{
			return ( Alias.Length == 0 );
		}
		#endregion

		#region Resolving related methods

		/// <summary>
		/// Additional internal flag for Resolved state.
		/// </summary>
		[IgnoreDataMember]
		private bool resolvedOnce;

		/// <summary>
		/// Is this identifier is resolved to valid ID.
		/// </summary>
		[IgnoreDataMember]
		public bool Resolved { get { return ( this.ID != UnresolvedID && resolvedOnce ); } }

		/// <summary>
		/// Is this MFIdentifier empty == unset value.
		/// </summary>
		public bool Empty { get { return ( this.originalID == UnresolvedID && this.alias.Length == 0 ); } }

		/// <summary>
		/// Internal resolving method.
		/// </summary>
		/// <param name="vault">The vault where to resolve.</param>
		/// <returns>true if handled, false is nothing done.</returns>
		protected bool _Resolve( Vault vault )
		{
			// Catch protect since our resolving methods may return exceptions we dont want to pass thru.
			try
			{
				// Based on type, select the resolving method.
				if( this.type == typeof( View ) )
				{
					// View can be GUID or ID.
					if( this.GUID.Length > 0 )
					{
						// Find the View using GUID.
						this.ID = vault.ViewOperations.GetViewIDByGUID( this.GUID );
					}
					else
					{
						// ID is verified trying to get the view.
						this.ID = vault.ViewOperations.GetView( this.originalID ).ID;

						// GetView might return view other than requested, set back if not the requested.
						if( this.ID != this.originalID )
							this.ID = UnresolvedID;
					}
				}
				else if( this.type == typeof( ClassGroup ) )
				{
					// ClassGroup has only GUID and ID.
					if( this.GUID.Length > 0 )
					{
						// Find the group using GUID.
						this.ID = vault.ClassGroupOperations.GetClassGroupIDByGUID( this.GUID );
					}
					else
					{
						// ID is verified trying to get the group.
						this.ID = vault.ClassGroupOperations.GetClassGroup( ( int )MFBuiltInObjectType.MFBuiltInObjectTypeDocument, this.originalID ).ID;
					}
				}
				else if( this.type == typeof( UserGroup ) )
				{
					// UserGroup has GUID, Alias and ID, search based on given type.
					if( this.GUID.Length > 0 )
						this.ID = vault.UserGroupOperations.GetUserGroupIDByGUID( this.GUID );
					else if( this.Alias.Length > 0 )
						this.ID = vault.UserGroupOperations.GetUserGroupIDByAlias( this.Alias );
					else
						this.ID = vault.UserGroupOperations.GetUserGroup( this.originalID ).ID;

				}
				else if( this.Alias.Length == 0 && this.originalID != UnresolvedID )
				{
					// Regardles of the type, IDs need to be resolved our own.
					if( this.type == typeof( PropertyDef ) )
						this.ID = vault.PropertyDefOperations.GetPropertyDef( this.originalID ).ID;
					if( this.type == typeof( ObjType ) )
						this.ID = vault.ObjectTypeOperations.GetObjectType( this.originalID ).ID;
					if( this.type == typeof( ObjectClass ) )
						this.ID = vault.ClassOperations.GetObjectClass( this.originalID ).ID;
					if( this.type == typeof( Workflow ) )
						this.ID = vault.WorkflowOperations.GetWorkflowForClient( this.originalID ).ID;
					if( this.type == typeof( NamedACL ) )
						this.ID = vault.NamedACLOperations.GetNamedACL( this.originalID ).ID;
				}
				else
				{
					// We are not a type we handle ourselves, return false.
					return false;
				}
			}
			catch( Exception )
			{
				// Catch exception and set us unresolved.
				this.ID = UnresolvedID;
			}

			// Mark us once resolved.
			this.resolvedOnce = true;

			// Return at here means we've tried to resolve.
			return true;
		}

		/// <summary>
		/// Update this MFIdentifier to contain resolved ID of the alias, if it is not already resolved.
		/// Returns itself to allow command chaining.
		/// </summary>
		/// <param name="vault">The vault where to resolve.</param>
		/// <param name="targetType">Type of the object</param>
		/// <returns>this MFIdentifier</returns>
		public MFIdentifier Resolve( Vault vault, Type targetType )
		{
			// Dont resolve if already done.
			if( this.ID != UnresolvedID && this.resolvedOnce )
				return this;

			// Store the data type.
			this.type = targetType;

			// Use our internal resolver, and return if succeeded.
			if( this._Resolve( vault ) )
				return this;

			// Still unresolved, use vault.ResolveID if we have alias.
			if( this.Alias.Length > 0 )
				this.ID = vault.ResolveID( this.type, this.Alias );

			// Mark us once resolved.
			this.resolvedOnce = true;

			// Return this object
			return this;
		}

		/// <summary>
		/// Last used target type.
		/// </summary>
		protected Type type;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MFIdentifier()
		{
			// Set empty values.
			this.alias = "";
			this.ID = UnresolvedID;
			this.originalID = UnresolvedID;
			this.resolvedOnce = false;
		}

		/// <summary>
		/// Constructor to unknown anytype object.
		/// </summary>
		/// <param name="source">Source data object of many type.</param>
		public MFIdentifier( object source )
		{
			this.Set( source );
		}

		/// <summary>
		/// Constructor with known integer ID value.
		/// </summary>
		/// <param name="id">Known ID value.</param>
		public MFIdentifier( int id ) { this.alias = ""; this.ID = id; this.originalID = id; this.resolvedOnce = false; }

		/// <summary>
		/// Constructor with known string value.
		/// </summary>
		/// <param name="str">Known alias string.</param>
		public MFIdentifier( string str ) { this.alias = ( str != null ? str : "" ); this.ID = UnresolvedID; this.originalID = UnresolvedID; this.resolvedOnce = false; }

		/// <summary>
		/// Implicit conversion to int identifier, allowing direct use inplace of int.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns>ID</returns>
		public static implicit operator int( MFIdentifier identifier )
		{
			// If we are not resolved, throw exception.
			if( identifier.ID == -1 )
			{
				// Throw exception with all available information.
				if( identifier.type != null )
					throw new ApplicationException( "Vault element not found. Type:" + identifier.type.ToString() + " Alias:" + identifier.Alias );
				else
					throw new ApplicationException( "Vault element not found. Alias:" + identifier.Alias );
			}

			// Valid case, return our resolved ID.
			return identifier.ID;
		}

		/// <summary>
		/// Original value string value can be got by explicit request.
		/// Allowing conversion and assignment into string.
		/// <c>string s = (string)SomeIdent;</c>
		/// If only ID number available, returns it as a string.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns>alias</returns>
		public static explicit operator string( MFIdentifier identifier )
		{
			// Return the best choice as string.
			if( identifier.Alias.Length > 0 ) return identifier.Alias;
			if( identifier.originalID != -1 ) return identifier.originalID.ToString();
			if( identifier.ID == -1 ) return identifier.Alias;
			return identifier.ID.ToString();
		}

		/// <summary>
		/// Helper function able to set the identifier value to almost any object type.
		/// Accepts MFIdentifier, string, int, enum, and other that cast to string.
		/// Returns itself to allow command chaining.
		/// </summary>
		/// <remarks>Note that using with integer like object will clear the alias, unlike setting the .ID directly.</remarks>
		/// <param name="anyObject">Source of any type.</param>
		/// <returns>this</returns>
		public MFIdentifier Set( object anyObject )
		{
			// Set members based on type of the argument.
			if( anyObject == null )
			{
				// Null object is set as empty.
				this.Alias = "";
				this.originalID = UnresolvedID;
				return this;
			}

			// MFIdentifier type.
			MFIdentifier source = anyObject as MFIdentifier;
			if( source != null )
			{
				// anyObject is MFIdentifier, copy values into this.
				this.alias = source.alias;
				this.ID = source.ID;
				this.originalID = source.originalID;
				this.resolvedOnce = source.resolvedOnce;
			}
			else if( anyObject is string )
			{
				// String is set to Alias, clearing old ID.
				this.Alias = ( string )anyObject;
				this.originalID = UnresolvedID;
			}
			else if( anyObject is int )
			{
				// Integer is treated as ID, clearing old Alias.
				this.alias = "";
				this.ID = ( int )anyObject;
				this.originalID = this.ID;
			}
			else if( anyObject is long )
			{
				// Long integer is treated as ID, clearing old Alias.
				this.alias = "";
				this.ID = ( int )( long )anyObject;
				this.originalID = this.ID;
			}
			else if( anyObject.GetType().IsEnum )
			{
				// Enumeration is integer value, is treated as ID, clearing old Alias.
				this.alias = "";
				this.ID = ( int )anyObject;
				this.originalID = this.ID;
			}
			else
			{
				// No more known options, try to cast string.
				this.Alias = ( string )anyObject;
				this.originalID = UnresolvedID;
			}

			// Return ourself to allow chaining.
			return this;
		}

		/// <summary>
		/// Returns this instance of Alias value, if not avail, returns string of ID.
		/// </summary>
		public override string ToString()
		{
			return ( string )this;
		}

		#region Static Implicit "constructors"
		/// <summary>
		/// Implicit conversion of string to MFIdentifier.
		/// Allowing: <c>MFIdentifier x = "string";</c>
		/// </summary>
		/// <param name="str"></param>
		public static implicit operator MFIdentifier( string str ) { return new MFIdentifier( str ); }

		/// <summary>
		/// Implicit conversion of int to Ident, allowing: <c>MFIdentifier x = 109;</c>
		/// </summary>
		/// <param name="id"></param>
		public static implicit operator MFIdentifier( int id ) { return new MFIdentifier( id ); }

		/// <summary>
		/// Implicit conversion of long int to Ident, allowing: <c>MFIdentifier x = 109;</c>
		/// </summary>
		/// <param name="id"></param>
		public static implicit operator MFIdentifier( long id ) { return new MFIdentifier( ( int )id ); }

		#endregion
	}

}
