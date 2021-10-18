using DicomLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LocalClient
{
	internal class CsvUidMapProvider : IUidMapProvider
	{
		public const string FileName = "uidmap.csv";
		private readonly string _csvFileName;
		private readonly IList<UidMapEntity> _uids;

		public CsvUidMapProvider(string csvPath)
		{
			_csvFileName = Path.Join(csvPath, FileName);
			_uids = ReadCsv(_csvFileName);
		}

		private IList<UidMapEntity> ReadCsv(string csvFileName)
		{
			if (File.Exists(csvFileName))
			{
				return File.ReadAllLines(csvFileName)
					.Select(line => line.Split(','))
					.Select(x => new UidMapEntity()
					{
						PartitionKey = x[0],
						RowKey = x[1],
						RedactedUid = x[2],
						InstitutionId = x[3]
					})
					.ToList();
			}
			else
			{
				return new List<UidMapEntity>();
			}
		}

		public string GetRedactedUid(string dicomTag, string originalUid)
		{
			// TODO: Look up
			return DicomHelper.GenerateNewUid();
		}

		public void SetRedactedUid(string dicomTag, string originalUid, string redactedUid, string institutionId)
		{
			// TODO: Persist in mem and CSV
		}
	}
}
