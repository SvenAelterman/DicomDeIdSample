using Azure;
using Azure.Data.Tables;
using System;

namespace DicomLib
{
	public class UidMapEntity : ITableEntity
	{
		public UidMapEntity() { }

		/// <summary>
		/// Creates a new instance of the <see cref="UidMapEntity"/> class with the specified values.
		/// </summary>
		/// <param name="partitionKey">The DICOM tag to which this mapping applies.</param>
		/// <param name="rowKey">The original UID to which this mapping applies.</param>
		/// <param name="redactedUid">The redacted UID value.</param>
		/// <param name="institutionId">(optional) The institution ID to which this mapping applies.</param>
		public UidMapEntity(string partitionKey, string rowKey, string redactedUid, string institutionId = null)
		{
			PartitionKey = partitionKey;
			RowKey = rowKey;
			RedactedUid = redactedUid;
			InstitutionId = institutionId;
		}

		// Partition Key is the dicom tag (as a string like "(1234,5678)")
		// Row Key is the original DICOM UID

		public string DicomTag
		{
			get { return PartitionKey; }
			set { PartitionKey = value; }
		}

		public string OriginalUid
		{
			get { return RowKey; }
			set { RowKey = value; }
		}

		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }
		public string RedactedUid { get; set; }
		public string InstitutionId { get; set; }
	}
}
