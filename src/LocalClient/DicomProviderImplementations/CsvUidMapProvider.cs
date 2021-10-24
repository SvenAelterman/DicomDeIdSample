using DicomLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LocalClient
{
	internal class CsvUidMapProvider : IUidMapProvider
	{
		public const string DefaultFileName = "uidmap.csv";
		private readonly string _csvFileName;
		//private readonly IList<UidMapEntity> _uids;
		//private readonly Dictionary<UidMapLocator, UidMapEntity> _uidsD;
		private readonly IDictionary<UidMapLocator, UidMapEntity> _uidsM;

		public CsvUidMapProvider(string csvPath)
		{
			_csvFileName = Path.Join(csvPath, DefaultFileName);
			_uidsM = ReadCsv(_csvFileName);
		}

		private IDictionary<UidMapLocator, UidMapEntity> ReadCsv(string csvFileName)
		{
			if (File.Exists(csvFileName))
			{
				return File.ReadAllLines(csvFileName)
					.Select(line => line.Split(','))
					.Select(x => new UidMapEntity()
					{
						PartitionKey = x[0],    // DicomTag
						RowKey = x[1],          // Original UID
						RedactedUid = x[2],
						InstitutionId = x[3]
					})
					.ToDictionary(k => new UidMapLocator(k.PartitionKey, k.RowKey),
						new UidMapLocatorEqualityComparer());
			}
			else
			{
				// TODO: Create file and keep handle open?
				return new Dictionary<UidMapLocator, UidMapEntity>(new UidMapLocatorEqualityComparer());
			}
		}

		public string GetRedactedUid(string dicomTag, string originalUid)
		{
			UidMapLocator Locator = new UidMapLocator(dicomTag, originalUid);

			if (_uidsM.ContainsKey(Locator))
				return _uidsM[Locator].RedactedUid;
			else
				return null;
		}

		public void SetRedactedUid(string dicomTag, string originalUid, string redactedUid, string institutionId)
		{
			// TODO: Async persist CSV

			UidMapLocator Locator = new UidMapLocator(dicomTag, originalUid);

			if (!_uidsM.ContainsKey(Locator))
			{
				_uidsM.Add(Locator, new UidMapEntity(dicomTag, originalUid, redactedUid, institutionId));
			}
			else
			{
				UidMapEntity Existing = _uidsM[Locator];

				if (!Existing.RedactedUid.Equals(redactedUid))
					throw new UidConsistencyException();
			}
		}
	}
}
