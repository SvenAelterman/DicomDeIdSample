using Dicom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DicomLib
{
	/// <summary>
	/// Implementation of IDicomLib for the UMB NCAA COVID-19 Cardio research project.
	/// Uses the third-party open source Fellow Oak DICOM (fodicom) library.
	/// https://github.com/fo-dicom/fo-dicom
	/// </summary>
	public class FODicomWrapper : IDicomLib
	{
		/// <summary>
		/// De-identifies the specified DICOM file by replacing the values in the specified tags with the specified value.
		/// Only tags with non-empty values will be replaced.
		/// </summary>
		/// <param name="dicom">The DICOM file.</param>
		/// <param name="replaceValue">The value to use as the replacement value.</param>
		/// <param name="tagsToProcess">A list of DICOM tags (in the format of group,element) to be processed.</param>
		/// <param name="writer">Destination for verbose output.</param>
		/// <returns></returns>
		/// <remarks>This function does not remove data from the image, only from the tags.</remarks>
		public Stream ProcessTags(Stream dicom, string replaceValue, IList<string> tagsToProcess, IVerboseWriter writer)
		{
			if (dicom == null) throw new ArgumentNullException(nameof(dicom));
			if (tagsToProcess == null) throw new ArgumentNullException(nameof(tagsToProcess));
			if (tagsToProcess.Count == 0) throw new ArgumentException($"{nameof(tagsToProcess)} should contain at least one element.");
			if (replaceValue == null) replaceValue = "<replaced>";

			List<DicomTag> Process = new List<DicomTag>();

			// Process the list of tags to process passed in as a IList<string> into fodicom objects
			foreach (string TagName in tagsToProcess)
			{
				Process.Add(DicomTag.Parse(TagName));
			}

			DicomFile df = DicomFile.Open(dicom, FileReadOption.ReadAll);

			// Must create a list and not IEnumerable (or single statement) because the enumeration will be modified
			IList<DicomItem> ToUpdate = df.Dataset
				.Where(ds => df.Dataset.GetValueCount(ds.Tag) > 0)
				.ToList();

			ToUpdate
				.Each(item => AddOrUpdateDicomItem(df.Dataset, item.ValueRepresentation, item.Tag, replaceValue));

			df.Dataset.Validate();
			Stream OutStream = new MemoryStream();
			df.Save(OutStream);

			OutStream.Position = 0;

			return OutStream;
		}

		private void AddOrUpdateDicomItem(DicomDataset dataset, DicomVR valueRepresentation, DicomTag tag, string replaceValue)
		{
			// Code smell
			// Also, can't use switch statement because DicomVR.CS is not a constant
			if (DicomVR.CS.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.DA.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.TM.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.UI.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.SQ.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.DS.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.IS.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.US.Code.Equals(valueRepresentation.Code))
			{ }
			else if (DicomVR.OW.Code.Equals(valueRepresentation.Code))
			{ }
			else
			// Attempt to update like a string
			{ dataset.AddOrUpdate(valueRepresentation, tag, replaceValue); }			
		}

		public Stream RemoveTags(Stream dicom, string replaceValue, IList<string> keepTags,
			IVerboseWriter writer)
		{
			if (dicom == null) throw new ArgumentNullException(nameof(dicom));

			List<DicomTag> Keep = new List<DicomTag>();

			// Process the list of tags to keep passed in as a IList<string> into fodicom objects
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

			// Create the list of tags to be updated/replaced/cleared that are "person names"
			// Deliberately don't include other tags for this sample
			foreach (var fmi in df.Dataset.Where(ds => ds.ValueRepresentation.Code == "PN"))
			{
				// TODO: Text Analytics here
				if (!Keep.Contains(fmi.Tag))
				{
					ToUpdate.Add(fmi.Tag);
				}
			}

			// Process each tag
			foreach (var tag in ToUpdate)
			{
				// TODO: Log original value, etc.
				df.Dataset.AddOrUpdate(new DicomPersonName(tag, replaceValue));
			}

#if VEROBSE
			// This is verbose output only, to validate
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
#endif
			df.Dataset.Validate();
			Stream OutStream = new MemoryStream();
			df.Save(OutStream);

			OutStream.Position = 0;

			return OutStream;
		}
	}
}
