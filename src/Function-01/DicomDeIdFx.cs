using DicomLib;
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
		/// <param name="outStream">The </param>
		/// <param name="log"></param>
		[FunctionName("DicomDeIdFx")]
		public static void Run([BlobTrigger("dicom-samples-id/{name}", Connection = "sourceBlobConnection")] Stream inStream,
			string name,
			// Write the output to the same filename in a different container in the same storage account
			[Blob("dicom-samples-deid/{name}", FileAccess.Write, Connection = "sourceBlobConnection")] Stream outStream,
			ILogger log)
		{
			log.LogInformation($"C# Blob trigger function Processed blob\n\tName:{name}\n\tSize: {inStream.Length} Bytes");

			IList<string> TagKeepList = new List<string>()
			{
				"0010,0020" // Patient ID, keep per Flywheel
			};

			IDicomLib lib = new FODicomWrapper();

			lib.RemoveTags(inStream, "<replaced>", TagKeepList, new VerboseLogger(log)).CopyTo(outStream);
		}
	}
}
