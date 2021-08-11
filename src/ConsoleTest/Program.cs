using DicomLib;
using Microsoft.Azure.Cosmos.Table;
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
			StreamReader file = new(".\\TestFiles\\img0001-28.9063 - id2.dcm");
			Stream ds = file.BaseStream;

			IConfiguration configuration = new ConfigurationBuilder()
				.AddJsonFile("appSettings.json")
				.Build();

			string StorageConnectionString = configuration["sourceConnection"];

			// Get a reference to the Azure Table in the specified Azure Storage account
			// For local testing, use Azurite
			CloudStorageAccount a = CloudStorageAccount.Parse(StorageConnectionString);
			CloudTable table = new CloudTableClient(a.TableStorageUri, a.Credentials).GetTableReference("dicomuidmap");
			IDicomLib dl = new FODicomWrapper(new AzureTableUidMapProvider(table), institutionId: "INS");

			Console.WriteLine("De-identifying");
			ds.Position = 0;

			// Call the helper library to de-identify the file
			_ = dl.ProcessTags(ds, null, DicomHelper.GetDefaultTags(), new ConsoleVerboseWriter());

			Console.Write("Done...");
			Console.ReadKey();
		}
	}
}
