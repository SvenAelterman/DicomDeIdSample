using Microsoft.Azure.Cosmos.Table;

namespace DicomLib
{
	public class UidMapEntity : TableEntity
	{
		public UidMapEntity() { }

		public UidMapEntity(string partitionKey, string rowKey, string redactedUid, string institutionId)
			: base(partitionKey, rowKey)
		{
			RedactedUid = redactedUid;
			InstitutionId = institutionId;
		}

		// Partition Key is the dicom tag (as a string like "1234,5678")
		// Row Key is the original DICOM UID

		public string RedactedUid { get; set; }
		public string InstitutionId { get; set; }
	}
}
