using Azure;
using Azure.Data.Tables;
using System;

namespace DicomLib
{
	public class UidMapEntity : ITableEntity
	{
		public UidMapEntity() { }

		public UidMapEntity(string partitionKey, string rowKey, string redactedUid, string institutionId)
		{
			PartitionKey = partitionKey;
			RowKey = rowKey;
			RedactedUid = redactedUid;
			InstitutionId = institutionId;
		}

		// Partition Key is the dicom tag (as a string like "(1234,5678)")
		// Row Key is the original DICOM UID

		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }
		public string RedactedUid { get; set; }
		public string InstitutionId { get; set; }
	}
}
