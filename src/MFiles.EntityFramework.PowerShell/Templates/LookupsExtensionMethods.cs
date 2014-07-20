using MFilesAPI;

namespace NAMESPACE
{
	/// <summary>
	/// Defines convenience methods for the Lookups interface.
	/// </summary>
	public static class LookupsExtensionMethods
	{

		/// <summary>
		/// Determines if the passed lookups contains all the same items as this one.
		/// </summary>
		/// <param name="lookups">The source lookups object.</param>
		/// <param name="otherLookups">The lookups object to compare the source with.</param>
		/// <returns>True if the source lookups object, and other lookups object have the same items.</returns>
		public static bool IsEqual(this Lookups lookups, Lookups otherLookups)
		{
			// If there are different counts, they are not equal.
			if (lookups.Count != otherLookups.Count)
				return false;

			// Loop through lookups in one list and makes sure it exists in the other.
			foreach (Lookup l in otherLookups)
			{
				if (lookups.GetLookupIndexByItem(l.Item) == -1)
					//A Lookup wasn't found in both. Not equal.
					return false;
			}

			// No problems found. They must be equal.
			return true;
		}

		/// <summary>
		/// Determines if the passed lookup has any items that are also in this one.
		/// </summary>
		/// <param name="lookups">The source lookups object.</param>
		/// <param name="otherLookups">The lookups object to compare the source with.</param>
		/// <returns>True if the source lookups object, has any items that are in the other lookups object.</returns>
		public static bool Intersects(this Lookups lookups, Lookups otherLookups)
		{
			// Loop over lookups in first list
			foreach (Lookup l in lookups)
			{
				// if we find the lookup item in the other list. There is an intersection!
				if (otherLookups.GetLookupIndexByItem(l.Item) != -1)
					return true;
			}

			// No intersection found if we made it here.
			return false;
		}

	}
}
