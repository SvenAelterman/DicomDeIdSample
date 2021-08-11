namespace DicomLib
{
	/// <summary>
	/// An implementation of this interface provides functionality to retrieve and set the redacted UID for a given DICOM UID.
	/// </summary>
	public interface IUidMapProvider
	{
		string GetRedactedUid(string dicomTag, string originalUid);
		void SetRedactedUid(string dicomTag, string originalUid, string redactedUid, string institutionId);
	}
}
