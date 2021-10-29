using DicomLib;
using System.Collections.Generic;
using System.Linq;

namespace LocalClient
{
	internal class CsvIdMapProvider : IIdMapProvider
	{
		private readonly IList<IdMapEntity> _idMapEntities;
		public const string DefaultPartitionKey = "local";

		public CsvIdMapProvider(string csvFileName)
		{
			_idMapEntities = ReadCsv(csvFileName);
		}

		/// <summary>
		/// Reads the specified CSV file and returns a list of <see cref="DicomLib.IdMapEntity"/>.
		/// </summary>
		/// <param name="csvFileName"></param>
		/// <returns></returns>
		private IList<IdMapEntity> ReadCsv(string csvFileName)
		{
			return System.IO.File.ReadAllLines(csvFileName)
				.Select(line => line.Split(','))
				.Select(x => new IdMapEntity()
				{
					PartitionKey = DefaultPartitionKey,
					StudyId = x[0],
					RowKey = x[1]
				})
				.ToList();
		}

		public string GetStudyId(string _, string currentPatientID)
		{
			return _idMapEntities
				.Single(e => e.PartitionKey == DefaultPartitionKey
					&& e.RowKey == currentPatientID)
				.StudyId;
		}
	}
}
