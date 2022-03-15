using DicomLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalClient
{
	internal class CsvIdMapProvider : IIdMapProvider
	{
		public const string DefaultPartitionKey = "local";

		public CsvIdMapProvider(string csvFileName)
		{
			IdMap = ReadCsv(csvFileName);
		}

		public IList<IdMapEntity> IdMap { get; }

		/// <summary>
		/// Reads the specified CSV file and returns a list of <see cref="DicomLib.IdMapEntity"/>.
		/// </summary>
		/// <param name="csvFileName"></param>
		/// <returns></returns>
		private IList<IdMapEntity> ReadCsv(string csvFileName)
		{
			return System.IO.File.ReadAllLines(csvFileName)
				// Allow for comments
				.Where(line => !line.StartsWith('#'))
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
			try
			{
				return IdMap
					.Single(e => e.PartitionKey == DefaultPartitionKey
						&& e.RowKey == currentPatientID)
					.StudyId;
			}
			catch (InvalidOperationException ex) when (ex.Message.Contains("no matching element"))
			{
				throw new MissingPatientIdInIdMapException($"The ID map does not contain an entry for patient ID '{currentPatientID}'.", ex);
			}
		}
	}
}
