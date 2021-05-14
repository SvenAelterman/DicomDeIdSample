using Dicom;
using DicomLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleTest
{
	public static class Program
	{
		public static void Main()
		{
			Console.WriteLine("Hello World!");

			// TODO: Just find first file with .dcm extension in TestFiles directory
			StreamReader file = new(".\\TestFiles\\img0001-28.9063.dcm");
			Stream ds = file.BaseStream;
			IDicomLib dl = new FODicomWrapper();

			DicomFile df = DicomFile.Open(ds, FileReadOption.ReadAll);

			Console.WriteLine("Original Dataset");

			foreach (var ds1 in df.Dataset)
			{
				Console.WriteLine($"\t{ds1.Tag} {ds1.Tag.DictionaryEntry.Name}: {ds1.ValueRepresentation.Name} ({ds1.ValueRepresentation.ValueType}):");

				if (df.Dataset.TryGetString(ds1.Tag, out string Value))
				{
					Console.WriteLine($"\t\t{Value}");
				}
				else
				{
					Console.WriteLine("\t\t<not a string>");
				}
			}

			Console.WriteLine("De-identifying");
			ds.Position = 0;

			// Call the helper library to de-identify the file
			Stream NewStream = dl.RemoveTags(ds, "<replaced>", new List<string>() { "0010,0020" }, new ConsoleVerboseWriter());

			ds.Position = 0;
			df = DicomFile.Open(ds, FileReadOption.ReadAll);

			Console.WriteLine("Original Dataset");

			foreach (var ds1 in df.Dataset.Where(ds => ds.ValueRepresentation.Code == "PN"))
			{
				Console.WriteLine($"\t{ds1.Tag} {ds1.Tag.DictionaryEntry.Name}: {ds1.ValueRepresentation.Name} ({ds1.ValueRepresentation.ValueType}):");

				if (df.Dataset.TryGetString(ds1.Tag, out string Value))
				{
					Console.WriteLine($"\t\t{Value}");
				}
				else
				{
					Console.WriteLine("\t\t<not a string>");
				}
			}

			// Treat the new stream as a DicomFile
			DicomFile dfdeid = DicomFile.Open(NewStream, FileReadOption.ReadAll);

			Console.WriteLine("New dataset");

			// Get all Person Name tags
			foreach (var ds1 in dfdeid.Dataset.Where(ds => ds.ValueRepresentation.Code == "PN"))
			{
				Console.WriteLine($"\t{ds1.Tag} {ds1.Tag.DictionaryEntry.Name}: {ds1.ValueRepresentation.Name} ({ds1.ValueRepresentation.ValueType}):");

				if (dfdeid.Dataset.TryGetString(ds1.Tag, out string Value))
				{
					Console.WriteLine($"\t\t{Value}");
				}
				else
				{
					Console.WriteLine("\t\t<not a string>");
				}
			}

			Console.Write("Done...");
			Console.ReadKey();
		}
	}
}
