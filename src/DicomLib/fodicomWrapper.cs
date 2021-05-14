using Dicom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DicomLib
{
	public class FODicomWrapper : IDicomLib
	{
		public Stream RemoveTags(Stream dicom, string replaceValue, IList<string> keepTags,
			IVerboseWriter writer)
		{
			if (dicom == null) throw new ArgumentNullException(nameof(dicom));

			List<DicomTag> Keep = new List<DicomTag>();

			if (keepTags != null)
			{
				foreach (string TagName in keepTags)
				{
					Keep.Add(DicomTag.Parse(TagName));
				}
			}

			DicomFile df = DicomFile.Open(dicom, FileReadOption.ReadAll);
			int OutLength = dicom.Length > int.MaxValue ? int.MaxValue : (int)dicom.Length;

			List<DicomTag> ToUpdate = new List<DicomTag>();

			foreach (var fmi in df.Dataset.Where(ds => ds.ValueRepresentation.Code == "PN"))
			{
				if (!Keep.Contains(fmi.Tag))
				{
					ToUpdate.Add(fmi.Tag);
				}
			}

			foreach (var tag in ToUpdate)
			{
				// TODO: Log
				df.Dataset.AddOrUpdate(new DicomPersonName(tag, replaceValue));
			}

			df.Dataset.Validate();

			if (writer != null)
			{
				writer.Write("From wrapper");

				foreach (var ds1 in df.Dataset.Where(ds => ds.ValueRepresentation.Code == "PN"))
				{
					writer.Write($"\t{ds1.Tag} {ds1.Tag.DictionaryEntry.Name}: {ds1.ValueRepresentation.Name} ({ds1.ValueRepresentation.ValueType}):");

					if (df.Dataset.TryGetString(ds1.Tag, out string Value))
					{
						writer.Write($"\t\t{Value}");
					}
					else
					{
						writer.Write("\t\t<not a string>");
					}
				}
			}

			Stream OutStream = new MemoryStream();
			df.Save(OutStream);

			//OutStream.Flush();
			OutStream.Position = 0;

			return OutStream;
		}
	}
}
