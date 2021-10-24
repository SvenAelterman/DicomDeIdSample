namespace LocalClient
{
	internal class UidMapLocator
	{
		public UidMapLocator(string dicomTag, string originalUid)
		{
			DicomTag = dicomTag;
			OriginalUid = originalUid;
		}

		public string DicomTag { get; set; }
		public string OriginalUid { get; set; }
	}
}
