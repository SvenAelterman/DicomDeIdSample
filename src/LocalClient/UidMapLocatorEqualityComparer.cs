using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LocalClient
{
	internal class UidMapLocatorEqualityComparer : EqualityComparer<UidMapLocator>
	{
		public UidMapLocatorEqualityComparer()
		{

		}

		public override bool Equals([AllowNull] UidMapLocator x, [AllowNull] UidMapLocator y)
		{
			if (x == null && y == null)
				return true;
			else if (x == null || y == null)
				return false;

			return x.DicomTag == y.DicomTag
				&& x.OriginalUid == y.OriginalUid;
		}

		public override int GetHashCode([DisallowNull] UidMapLocator obj)
		{
			return obj.DicomTag.GetHashCode() ^ obj.OriginalUid.GetHashCode();
		}
	}
}
