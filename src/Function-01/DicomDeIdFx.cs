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
		/// <param name="idMapTable">The Azure Table containing between patient medical record number and study ID.</param>
		/// <param name="uidMapTable">The Azure Table containing the map of UIDs.</param>
		/// <param name="log"></param>
		[FunctionName("DicomDeIdFx")]
		public static void Run(
			[EventGridTrigger] EventGridEvent eventGridEvent,
			// The Function app will access the source blob referenced by the Event Grid's data.url property using a system managed identity
			[Blob("{data.url}", FileAccess.Read)] Stream inStream,
			[Table("dicomdeid", Connection = "sourceConnection")] CloudTable idMapTable,
			[Table("dicomuidmap", Connection = "sourceConnection")] CloudTable uidMapTable,
			ILogger log)
		{
			// TODO: consider using StorageBlobCreatedEventData class (deserialize eventGridEvent.Data JSON)

			string SourceBlobUrl = ((dynamic)eventGridEvent.Data).url;
			BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(SourceBlobUrl));

			string SourceBlobPathAndName = blobUriBuilder.BlobName;
			string StorageAccountName = blobUriBuilder.AccountName;
			// Extract institution (partition key for table) from storage account name (last three characters of storage account name)
			string InstitutionId = StorageAccountName.Substring(StorageAccountName.Length - 3, 3);

			if (inStream == null) throw new ArgumentNullException(nameof(inStream));

			// TODO: Define custom class with tag ID and action ("default", "clear", "redact")?
			IList<DicomTagProcessTask> TagProcessList = DicomHelper.GetDefaultTags();

			IDicomLib lib = new FODicomWrapper(new AzureTableUidMapProvider(uidMapTable), InstitutionId);
			IVerboseWriter writer = new VerboseLogger(log);

			// Retrieve the subject's current medical record number before anonymization
			string CurrentPatientId = lib.GetPatientId(inStream);
			// Construct query to retrieve the subject's study ID from the Azure Table
			TableOperation GetStudyId = TableOperation.Retrieve<IdMapEntity>(InstitutionId, CurrentPatientId);

			if (GetStudyId != null)
			{
				// TODO: Consider creating an overarching function in IDicomLib that performs the entire process

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
