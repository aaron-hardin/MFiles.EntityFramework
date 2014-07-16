using System;

namespace testpkg.PowerShell.Templates
{
	public class ObjVerEx : IDisposable
	{
		private bool checkIn;
		private bool propertiesChanged = false;
		private bool readOnly;

		public ObjVerEx(bool readOnly = false)
		{
			this.readOnly = readOnly;
			checkIn = !readOnly && StartRequireCheckedOut();
		}

		public bool StartRequireCheckedOut()
		{
			throw new NotImplementedException();
		}

		public void EndRequireCheckedOut(bool checkIn)
		{
			throw new NotImplementedException();
			if (readOnly)
				return;

			if (propertiesChanged)
			{
				//check in
			}
			else
			{
				//save properties
			}
		}

		public void Dispose()
		{
			EndRequireCheckedOut(checkIn);
		}

		public virtual int ObjType { get; set; }

	}
}
