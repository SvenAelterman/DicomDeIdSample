using Azure;
using Azure.Data.Tables;
using System;

namespace DicomLib
{
	public class IdMapEntity : ITableEntity
	{
		// Partition Key is the institution ID (3 chars)
		// Row Key is the subject's MRN

		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }
		public string StudyId { get; set; }
	}
}
