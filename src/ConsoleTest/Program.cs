using Azure.Data.Tables;
using DicomLib;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ConsoleTest
{
	public static class Program
	{
		public static void Main()
		{
			// TODO: Just find first file with .dcm extension in TestFiles directory
			StreamReader file = new(".\\TestFiles\\img0001-28.9063.dcm");
			Stream ds = file.BaseStream;

			IConfiguration configuration = new ConfigurationBuilder()
				.AddJsonFile("appSettings.json")
				.Build();

			string StorageConnectionString = configuration["sourceConnection"];

			// Get a reference to the Azure Table in the specified Azure Storage account
			// For local testing, use Azurite
			TableClient table = new TableClient(StorageConnectionString, "dicomuidmap");
			IDicomLib dl = new FODicomWrapper(new AzureTableUidMapProvider(table), institutionId: "INS");

			Console.WriteLine("De-identifying");
			ds.Position = 0;

			// Call the helper library to de-identify the file
			Stream outp = dl.ProcessTags(ds, null, DicomHelper.GetDefaultTags(), new ConsoleVerboseWriter());
			outp.Position = 0;

			using StreamWriter writer = new(".\\TestFiles\\processed.dcm", append: false);
			outp.CopyTo(writer.BaseStream);
			writer.Flush();

			Console.Write("Done...");
			Console.ReadKey();
		}
	}
}
