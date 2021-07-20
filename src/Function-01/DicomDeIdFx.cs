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
		/// <param name="outStream">The output blob.</param>
		/// <param name="log"></param>
		[FunctionName("DicomDeIdFx")]
		public static void Run([BlobTrigger("dicom-samples-id/{name}", Connection = "sourceBlobConnection")] Stream inStream,
			string name,
			// Write the output to the same filename in a different container in the same storage account
			[Blob("dicom-samples-deid/{name}", FileAccess.Write, Connection = "sourceBlobConnection")] Stream outStream,
			ILogger log)
		{
			log.LogInformation($"C# Blob trigger function processing blob\n\tName:{name}\n\tSize: {inStream.Length} Bytes");

			//IList<string> TagKeepList = new List<string>()
			//{
			//	"0010,0020" // Patient ID, keep per Flywheel
			//};
			IList<string> TagProcessList = new List<string>()
			{
				"0002,0000",
				"0002,0003",
				"0002,0016",
				"0008,0018",
				"0008,0050",
				"0008,0090",
				"0010,0030",
				"0010,1010",
				"0010,0010",
				"0010,0020",
				"0010,1010",
				"0018,1000",
				"0010,21C0",
				"0008,0080",
				"0008,0081",
				"0008,0090",
				"0008,1010",
				"0008,1070",
				"0040,0275",
				"FFFE,E000",
				"0040,0007",
				"FFFE,E00D",
				"FFFE,E0DD",
				// Tags that might need to be hashed
				"0020,000D",
				"0020,000E",
				"0040,1001"
			};

			IDicomLib lib = new FODicomWrapper();

			// TODO: Hash certain tags if required by Flywheel (0020,000D ; 0020,000E ; 0040,1001)
			// TODO: Add patient ID to 0010,0020

			//lib.RemoveTags(inStream, "<replaced>", TagKeepList, new VerboseLogger(log)).CopyTo(outStream);
			lib.ProcessTags(inStream, "<replaced>", TagProcessList, new VerboseLogger(log)).CopyTo(outStream);
		}
	}
}
