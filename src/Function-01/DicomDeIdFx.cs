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
				"0010,0010",	// Set in sample (study's ID), PN
				"0010,0020",	// Added to sample, LO
				"0018,1000",	// Can't add to sample (retired tag, per LEADTOOLS)
				"0010,21C0",	// Added to sample, US 0001
				"0008,0080",	// Added to sample, LO
				"0008,0081",	// Added to sample, ST
				"0008,1010",	// Added to sample, SH
				"0008,1070",	// Added to sample, PN
				"0040,0275",	// Unable to add to sample
				"FFFE,E000",	// Added to sample, OB ?
				"0040,0007",	// Added to sample, LO
				"FFFE,E00D",	// ?
				"FFFE,E0DD",	// ?
				// Tags that might need to be hashed
				"0020,000D",
				"0020,000E",
				"0040,1001"
			};

			IDicomLib lib = new FODicomWrapper();

			// TODO: Hash certain tags if required by Flywheel (0020,000D ; 0020,000E ; 0040,1001)

			// TODO: Add patient ID to 0010,0020

			lib.ProcessTags(inStream, null, TagProcessList, new VerboseLogger(log)).CopyTo(outStream);
		}
	}
}
