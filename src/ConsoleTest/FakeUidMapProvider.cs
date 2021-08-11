using DicomLib;

namespace ConsoleTest
{
	/// <summary>
	/// Mock implementation of the IUidMapProvider interface for quick local testing.
	/// </summary>
	internal class FakeUidMapProvider : IUidMapProvider
	{
		public string GetRedactedUid(string dicomTag, string originalUid)
		{
			return "2.25.1.2.3.4.5";
		}

		public void SetRedactedUid(string dicomTag, string originalUid, string redactedUid, string institutionId)
		{
		}
	}
}