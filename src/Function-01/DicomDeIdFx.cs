using Azure.Storage.Blobs;
using DicomLib;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Function_01
{
	public static class DicomDeIdFx
	{
		/// <summary>
		/// Processes the tags in the DICOM file provided by the EventGridTrigger and attempts to remove identifying information.
		/// </summary>
		/// <param name="eventGridEvent"></param>
		/// <param name="inStream">The input DICOM file from the Blob storage.</param>
		/// <param name="outStream">The output blob.</param>
		/// <param name="idMapTable"></param>
		/// <param name="log"></param>
		[FunctionName("DicomDeIdFx")]
		public static void Run(
			[EventGridTrigger] EventGridEvent eventGridEvent,
			// The Function app will access the source blob referenced by the Event Grid's data.url property using a system managed identity
			[Blob("{data.url}", FileAccess.Read)] Stream inStream,
			// The Azure Table where the map between patient medical record number and study ID is stored
			[Table("dicomdeid", Connection = "sourceConnection")] CloudTable idMapTable,
			ILogger log)
		{
			// TODO: consider using StorageBlobCreatedEventData class
			string SourceBlobUrl = ((dynamic)eventGridEvent.Data).url;
			BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(SourceBlobUrl));

			string SourceBlobPathAndName = blobUriBuilder.BlobName;
			string StorageAccountName = blobUriBuilder.AccountName;
			// Extract institution (partition key for table) from storage account name (last three characters of storage account name)
			string InstitutionId = StorageAccountName.Substring(StorageAccountName.Length - 3, 3);

			log.LogInformation($"C# Event Grid trigger function processing blob\r\n\tName: {SourceBlobPathAndName}\r\n\tSize: {inStream?.Length} Bytes.");

			if (inStream == null) throw new ArgumentNullException(nameof(inStream));

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
			IVerboseWriter writer = new VerboseLogger(log);

			// Retrieve the subject's current medical record number before anonymization
			string CurrentPatientId = lib.GetPatientId(inStream);
			// Construct query to retrieve the subject's study ID from the Azure Table
			TableOperation GetStudyId = TableOperation.Retrieve<IdMapEntity>(InstitutionId, CurrentPatientId);

			if (GetStudyId != null)
			{
				// TODO: Hash certain tags as required by Flywheel (0020,000D ; 0020,000E ; 0040,1001)

				// Process the tags for anonymization
				Stream ModifiedStream = lib.ProcessTags(inStream, null, TagProcessList, writer);

				// Retrieve the subject's study ID
				// TODO: Create a cache?
				string NewPatientId = ((IdMapEntity)idMapTable.ExecuteAsync(GetStudyId).Result.Result).StudyId;

				// Pass in the already modified (de-identified) stream to the method to set the patient ID to the study ID
				// Add subject's study ID to 0010,0020
				ModifiedStream = lib.SetPatientId(ModifiedStream, NewPatientId, writer);

				string TargetBlobPathAndName = CreateTargetBlobPath(SourceBlobPathAndName, NewPatientId, InstitutionId);

				BlobServiceClient TargetStorageAccount = new BlobServiceClient(Environment.GetEnvironmentVariable("targetBlobConnection"));
				BlobContainerClient Container = TargetStorageAccount.GetBlobContainerClient(Environment.GetEnvironmentVariable("targetContainerName"));

				Container.CreateIfNotExists();

				BlobClient c = Container.GetBlobClient(TargetBlobPathAndName);

				// TODO: Consider adding metadata for lineage?
				//BlobUploadOptions opt = new BlobUploadOptions();
				//opt.Metadata.Add("processed-by", "ms-us-edu-dicomdeid-sample");

				c.Upload(ModifiedStream, overwrite: true);
			}
			else
			{
				// Log error
				log.LogError("Could not retrieve study ID for patient.");
			}
		}

		/// <summary>
		/// Turns the source blob path from
		/// LAST_INITIAL_AGE/study/series/file001.dcm
		/// into
		/// institutionid/patientstudyid/study/series/file001.dcm
		/// </summary>
		/// <param name="sourceBlobPath"></param>
		/// <param name="newPatientId"></param>
		/// <param name="institutionId"></param>
		/// <returns></returns>
		private static string CreateTargetBlobPath(string sourceBlobPath, string newPatientId, string institutionId)
		{
			// Site should not be part of this, because the storage accounts indicates the site. In which case we need to add an array element in the front
			IList<string> SourceSplit = sourceBlobPath.Split('/').ToList();
			// The first element (first folder) is the subject name, or ID, or ... Replace it with the NewPatientId
			SourceSplit[0] = newPatientId;
			// Insert a new folder in front that is the institution ID
			SourceSplit.Insert(0, institutionId);
			return string.Join('/', SourceSplit);
		}
	}
}
