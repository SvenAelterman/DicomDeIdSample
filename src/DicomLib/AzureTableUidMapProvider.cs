//using Microsoft.Azure.Cosmos.Table;
using Azure;
using Azure.Data.Tables;
using System;

namespace DicomLib
{
	/// <summary>
	/// An implementation of IUidMapProvider that retrieves and sets the redacted UID from Azure Table storage.
	/// </summary>
	public class AzureTableUidMapProvider : IUidMapProvider
	{
		private readonly TableClient _table;

		public AzureTableUidMapProvider(TableClient table)
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

			try
			{
				Response<UidMapEntity> r = _table.GetEntity<UidMapEntity>(dicomTag, originalUid);

				if (r.GetRawResponse().Status < 400
					&& r.Value != null)
				{
					return r.Value.RedactedUid;
				}
				else
				{
					throw new InvalidOperationException("Exception while retrieving a redacted UID.");
				}
			}
			catch (RequestFailedException ex) when (ex.Status == 404)
			{
				return null;
			}
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
			Response r = _table.AddEntity(e);

			if (r.Status >= 400) throw new InvalidOperationException($"Could not upsert {e} due to {r.ReasonPhrase} ({r.Status})");
		}
	}
}
