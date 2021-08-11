using Microsoft.Azure.Cosmos.Table;
using System;

namespace DicomLib
{
	/// <summary>
	/// An implementation of IUidMapProvider that retrieves and sets the redacted UID from Azure Table storage.
	/// </summary>
	public class AzureTableUidMapProvider : IUidMapProvider
	{
		private readonly CloudTable _table;

		public AzureTableUidMapProvider(CloudTable table)
		{
			_table = table ?? throw new ArgumentNullException(nameof(table));
		}

		/// <summary>
		/// Retrieves the redacted UID associated with the specified DICOM tag and original UID.
		/// </summary>
		/// <param name="dicomTag"></param>
		/// <param name="originalUid"></param>
		/// <returns>The redacted UID associated with the specified UID, or null if there is no match in the map.</returns>
		public string GetRedactedUid(string dicomTag, string originalUid)
		{
			if (string.IsNullOrWhiteSpace(dicomTag)) throw new ArgumentNullException(nameof(dicomTag));
			if (string.IsNullOrWhiteSpace(originalUid)) throw new ArgumentNullException(nameof(originalUid));

			TableOperation GetUid = TableOperation.Retrieve<UidMapEntity>(dicomTag, originalUid);

			// This will return null if the table operation doesn't return anything
			return ((UidMapEntity)_table.ExecuteAsync(GetUid).Result?.Result)?.RedactedUid;
		}

		/// <summary>
		/// Stores the specified redacted UID in the map table with the DICOM tag
		/// </summary>
		/// <param name="dicomTag"></param>
		/// <param name="originalUid"></param>
		/// <param name="redactedUid"></param>
		/// <param name="institutionId"></param>
		public void SetRedactedUid(string dicomTag, string originalUid, string redactedUid, string institutionId)
		{
			if (string.IsNullOrWhiteSpace(dicomTag)) throw new ArgumentNullException(nameof(dicomTag));
			if (string.IsNullOrWhiteSpace(originalUid)) throw new ArgumentNullException(nameof(originalUid));
			if (string.IsNullOrWhiteSpace(redactedUid)) throw new ArgumentNullException(nameof(redactedUid));
			// It's OK if institutionID is null or empty

			UidMapEntity e = new UidMapEntity(dicomTag, originalUid, redactedUid, institutionId);
			TableOperation SetUid = TableOperation.Insert(e);

			_ = _table.ExecuteAsync(SetUid).Result.Result;
		}
	}
}
