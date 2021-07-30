using DicomLib;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;

namespace Function_01
{
	public static class DicomDeIdFx
	{
		/// <summary>
		/// Processes the tags in the DICOM file provided by the BlobTrigger and attempts to remove identifying information.
		/// </summary>
		/// <param name="inStream">The input DICOM file from the Blob storage.</param>
		/// <param name="name">The name of the DICOM blob.</param>
		/// <param name="outStream">The output blob.</param>
		/// <param name="log"></param>
		[FunctionName("DicomDeIdFx")]
		public static void Run([BlobTrigger("dicom-samples-id/{name}", Connection = "sourceBlobConnection")] Stream inStream,
			string name,
			// Write the output to the same filename in a different container in the same storage account
			[Blob("dicom-samples-deid/{name}", FileAccess.Write, Connection = "sourceBlobConnection")] Stream outStream,
			// TODO: Use Event Grid trigger for higher reliability
			//[EventGridTrigger] EventGridEvent eventGridEvent,
			//[Blob("{data.url}", FileAccess.Read)] Stream input,
			[Table("dicomdeid", Connection = "sourceBlobConnection")] CloudTable idMapTable,
			ILogger log)
		{
			log.LogInformation($"C# Blob trigger function processing blob\n\tName:{name}\n\tSize: {inStream.Length} Bytes");

			// TODO: Define custom class with tag ID and action ("default", "clear", "hash")?
			IList<string> TagProcessList = new List<string>()
			{
				"0002,0000",	// Can't add to sample
				"0002,0003",	// set in sample, UI
				"0002,0016",	// Added to sample, AE
				"0008,0018",	// set in sample, UI
				"0008,0050",	// Added to sample, SH
				"0008,0090",	// Added to sample, PN
				"0010,0030",	// Added to sample, DA
				"0010,1010",	// Added to sample, AS
				"0010,0010",	// Updated in sample, PN
				"0010,0020",	// Added to sample, LO
				"0018,1000",	// Add to sample, LO (retired tag, per LEADTOOLS)
				"0010,21C0",	// Added to sample, US 0001
				"0008,0080",	// Added to sample, LO
				"0008,0081",	// Added to sample, ST
				"0008,1010",	// Added to sample, SH
				"0008,1070",	// Added to sample, PN
				"0040,0007",	// Added to sample, LO
				"0040,0275",	// Unable to add to sample
				// Updates 2021-07-29
				"0008,1032",
				"0008,1048",
				"0008,1140",
				"0032,1032",
				"0032,1064",
				"0040,0253",
				"0020,0010",
				"0008,0023",
				"0008,0030",
				"0008,0031",
				"0008,0021",
				"0008,0020",
				"0008,0030",
				"0008,0031",
				"0008,0032",
				"0008,0033",
				"0020,0010",
				"0029,1009",
				"0029,1019",
				"0040,0244",
				"0040,0245",
				"0020,0052",
				// Tags that need to be hashed
				"0020,000D",
				"0020,000E",
				"0040,1001",
				// HACK: To try SQ
				//"0008,9215",
			};

			IDicomLib lib = new FODicomWrapper();
			// Retrieve the subject's current medical record number before anonymization
			string CurrentPatientId = lib.GetPatientId(inStream);

			// Process the tags for anonymization
			IVerboseWriter writer = new VerboseLogger(log);
			Stream ModifiedStream = lib.ProcessTags(inStream, null, TagProcessList, writer);

			// TODO: Hash certain tags as required by Flywheel (0020,000D ; 0020,000E ; 0040,1001)

			// Add subject's study ID to 0010,0020
			// TODO: extract institution (partition key for table) from storage account name (last three characters of storage account name)
			TableOperation GetStudyId = TableOperation.Retrieve<IdMapEntity>("umb", CurrentPatientId);
			if (GetStudyId != null)
			{
				// TODO: Create a cache?
				string NewPatientId = ((IdMapEntity)idMapTable.ExecuteAsync(GetStudyId).Result.Result).StudyId;
				ModifiedStream = lib.SetPatientId(ModifiedStream, NewPatientId, writer);
			}
			else
			{
				// Log error
				log.LogError("Could not retrieve study ID for patient.");
			}

			// TODO: Modify output path to use study ID instead of name/MRN folder
			ModifiedStream.CopyTo(outStream);
		}
	}
}
