using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using DicomLib;
//using Microsoft.Azure.Cosmos.Table;
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
			ILogger log,
			ExecutionContext exCtx)
		{
			if (eventGridEvent == null) throw new ArgumentNullException(nameof(EventGridEvent));

			string TargetStorageAccountUri = Environment.GetEnvironmentVariable("targetBlobUri");
			if (string.IsNullOrWhiteSpace(TargetStorageAccountUri)) throw new ArgumentNullException(nameof(TargetStorageAccountUri));

			string SourceBlobUrl = ((dynamic)eventGridEvent.Data).url;
			BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(SourceBlobUrl));

			string SourceBlobPathAndName = blobUriBuilder.BlobName;
			string StorageAccountName = blobUriBuilder.AccountName;

			// Extract institution (partition key for table) from storage account name (last three characters of storage account name)
			string InstitutionId = StorageAccountName.Substring(StorageAccountName.Length - 3, 3);
#if VERBOSE
			log.LogInformation($"Received event for file '{SourceBlobPathAndName}' for institution '{InstitutionId}'.");
#endif
			// Download the contents of the DICOM file
			BlobClient blobClient = new BlobClient(blobUriBuilder.ToUri(), _credential);
			using Stream BlobStream = blobClient.DownloadStreaming().Value.Content;

			// Copy to a memory stream, which can seek
			using MemoryStream DicomStream = new MemoryStream();
			BlobStream.CopyTo(DicomStream);

			if (DicomStream == null) throw new ArgumentNullException(nameof(DicomStream));
			if (!DicomStream.CanSeek) throw new InvalidOperationException("Stream must support seeking.");

			TableClient idMapTable = GetTableFromStorageAccount(blobUriBuilder, "dicomdeid");
			TableClient uidMapTable = GetTableFromStorageAccount(blobUriBuilder, "dicomuidmap");

			if (idMapTable == null) throw new ArgumentNullException(nameof(idMapTable));
			if (uidMapTable == null) throw new ArgumentNullException(nameof(uidMapTable));
#if VERBOSE
			log.LogInformation($"Table reference: {idMapTable.AccountName}");
#endif
			// Get the list of tags to process as part of de-identification
			IList<DicomTagProcessTask> TagProcessList = DicomHelper.GetDefaultTags();

			IVerboseWriter writer = new VerboseLogger(log);
			IDicomLib lib = new FODicomWrapper(new AzureTableUidMapProvider(uidMapTable), InstitutionId, writer);

			// Retrieve the subject's current medical record number before anonymization from the DICOM file
			string CurrentPatientId = lib.GetPatientId(DicomStream);
#if VERBOSE
			log.LogInformation($"Current patient ID: '{CurrentPatientId}'");
#endif
			// Construct query to retrieve the subject's study ID from the Azure Table
			// Retrieve the subject's study ID
			try
			{
				string NewPatientId = idMapTable.GetEntity<IdMapEntity>(InstitutionId, CurrentPatientId).Value?.StudyId;
#if VERBOSE
				log.LogInformation($"Study ID for '{CurrentPatientId}': '{NewPatientId}'");
#endif
				if (!string.IsNullOrWhiteSpace(NewPatientId))
				{
					// Process the tags for anonymization
					Stream ModifiedStream = lib.ProcessTags(DicomStream, null, TagProcessList);

					// Pass in the already modified (de-identified) stream to the method to set the patient ID to the study ID
					// Add subject's study ID to 0010,0020
					ModifiedStream = lib.SetPatientId(ModifiedStream, NewPatientId, writer);

					// Create the target path based on the source path
					string TargetBlobPathAndName = CreateTargetBlobPath(SourceBlobPathAndName, NewPatientId, InstitutionId);

					if (TargetStorageAccountUri.Substring(TargetStorageAccountUri.Length - 1, 1).Equals("/"))
						TargetStorageAccountUri = TargetStorageAccountUri.Substring(0, TargetStorageAccountUri.Length - 1);

					string TargetContainerUrl = $"{TargetStorageAccountUri}/{Environment.GetEnvironmentVariable("targetContainerName")}";
#if VERBOSE
					log.LogInformation($"Destination URI for anonymized DICOM: '{TargetContainerUrl}/{TargetBlobPathAndName}'");
#endif
					// Use URL and managed identity credential instead of full connection string
					BlobContainerClient TargetContainer = new BlobContainerClient(
						new Uri(TargetContainerUrl),
						_credential);

					TargetContainer.CreateIfNotExists();

					BlobClient c = TargetContainer.GetBlobClient(TargetBlobPathAndName);

					c.Upload(ModifiedStream, overwrite: true);

					// Add metadata for lineage
					IDictionary<string, string> Metadata = new Dictionary<string, string>()
					{
						{ "processed_by", "ms-us-edu-dicomdeid-sample" } ,
						{ "invocation_id", exCtx.InvocationId.ToString() },
					};
					c.SetMetadata(Metadata);
				}
				else
				{
					// Log error
					log.LogError($"Could not retrieve study ID for patient in file '{SourceBlobPathAndName}'.");
				}
			}
			catch (Azure.RequestFailedException ex) when (ex.Status == 404)
			{
				log.LogError($"Could not retrieve study ID for patient in file '{SourceBlobPathAndName}'.");
			}
		}

		private readonly static DefaultAzureCredential _credential = new DefaultAzureCredential(
			new DefaultAzureCredentialOptions()
			{
				// Local debuggin in VS failed without excluding these possible choices
				ExcludeAzureCliCredential = true,
				ExcludeAzurePowerShellCredential = true,
				ExcludeEnvironmentCredential = true,
				ExcludeInteractiveBrowserCredential = true,
				ExcludeVisualStudioCodeCredential = true,
			});

		/// <summary>
		/// Creates a table client referencing the specified table in the same storage account as the specified blob URI.
		/// </summary>
		/// <param name="blobUriBuilder">The blob URI.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <returns>A TableClient instance.</returns>
		private static TableClient GetTableFromStorageAccount(BlobUriBuilder blobUriBuilder, string tableName)
		{
			return new TableClient(GenerateTableUriFromBlobUriBuilder(blobUriBuilder), tableName, _credential);
		}

		/// <summary>
		/// Generates a URI pointing at the table endpoint of the specified storage account based on the specified blob URI.
		/// </summary>
		/// <param name="uriBuilder">The blob URI.</param>
		/// <returns>A URI to the table endpoint of the same storage account specified in the <paramref name="uriBuilder"/> parameter.</returns>
		private static Uri GenerateTableUriFromBlobUriBuilder(BlobUriBuilder uriBuilder)
		{
			return new Uri($"{uriBuilder.Scheme}://{uriBuilder.Host.Replace("blob", "table")}");
		}

		/// <summary>
		/// Turns the source blob path from
		/// LAST_FIRST_INITIAL_AGE/study/series/file001.dcm
		/// into
		/// institutionid/patientstudyid/study/series/file001.dcm
		/// </summary>
		/// <param name="sourceBlobPath">The path of the source blob (which triggered the function).</param>
		/// <param name="newPatientId">The value with wich to replace the subject folder name.</param>
		/// <param name="institutionId">The value to prepend as the new root folder.</param>
		/// <returns>The target blob path.</returns>
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
