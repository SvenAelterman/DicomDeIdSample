using CsvHelper;
using DicomLib;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LocalClient
{
	internal class CsvUidMapProvider : IUidMapProvider
	{
		public const string DefaultFileName = "uidmap.csv";
		private readonly string _csvFileName;
		private readonly IDictionary<UidMapLocator, UidMapEntity> _uids;

		public CsvUidMapProvider(string csvPath)
		{
			_csvFileName = csvPath;
			_uids = ReadCsv(_csvFileName);
		}

		public async Task WriteCSvAsync()
		{
			using (var Writer = new StreamWriter(_csvFileName))
			using (var Csv = new CsvWriter(Writer, CultureInfo.CurrentUICulture))
			{
				Csv.Context.RegisterClassMap<UidMapEntityCsvHelperMap>();

				await Csv.WriteRecordsAsync(_uids.Values);
			}
		}

		private IDictionary<UidMapLocator, UidMapEntity> ReadCsv(string csvFileName)
		{
			if (File.Exists(csvFileName))
			{
				using (var reader = new StreamReader(csvFileName))
				using (var Csv = new CsvReader(reader, CultureInfo.CurrentUICulture))
				{
					Csv.Context.RegisterClassMap<UidMapEntityCsvHelperMap>();

					return Csv.GetRecords<UidMapEntity>()
						.ToDictionary(k => new UidMapLocator(k.PartitionKey, k.RowKey),
							new UidMapLocatorEqualityComparer());
				}
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

			if (_uids.ContainsKey(Locator))
				return _uids[Locator].RedactedUid;
			else
				return null;
		}

		public void SetRedactedUid(string dicomTag, string originalUid, string redactedUid, string institutionId)
		{
			UidMapLocator Locator = new UidMapLocator(dicomTag, originalUid);

			if (!_uids.ContainsKey(Locator))
			{
				_uids.Add(Locator, new UidMapEntity(dicomTag, originalUid, redactedUid, institutionId));

				Task _ = WriteCSvAsync();
			}
			else
			{
				UidMapEntity Existing = _uids[Locator];

				if (!Existing.RedactedUid.Equals(redactedUid))
					throw new UidConsistencyException();
			}
		}
	}
}
