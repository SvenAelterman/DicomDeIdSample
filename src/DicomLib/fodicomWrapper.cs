using Dicom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DicomLib
{
	/// <summary>
	/// Implementation of IDicomLib for the UMB NCAA COVID-19 Cardio research project.
	/// Uses the third-party open source Fellow Oak DICOM (fodicom) library.
	/// https://github.com/fo-dicom/fo-dicom
	/// </summary>
	public class FODicomWrapper : IDicomLib
	{
		private const string PatientIdTagValue = "0010,0020";

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
			if (replaceValue == null) replaceValue = string.Empty;

			List<DicomTag> Process = new List<DicomTag>();

			// Process the list of tags to process passed in as a IList<string> into fodicom objects
			foreach (string TagName in tagsToProcess)
			{
				Process.Add(DicomTag.Parse(TagName));
			}

			dicom.Position = 0;
			DicomFile df = DicomFile.Open(dicom, FileReadOption.ReadAll);

			OutputDicomTags(writer, df.Dataset, Process, "BEFORE VALUES");

			// Must create a list and not IEnumerable (or single statement) because the enumeration will be modified when AddOrUpdate is called
			IList<DicomItem> ToUpdate = df.Dataset
				.Where(ds => df.Dataset.GetValueCount(ds.Tag) > 0
						&& Process.Contains(ds.Tag))
				.ToList();

			ToUpdate
				.Each(item => AddOrUpdateDicomItem(df.Dataset, item.ValueRepresentation, item.Tag, replaceValue));

			df.Dataset.Validate();

			OutputDicomTags(writer, df.Dataset, Process, "AFTER VALUES");

			Stream OutStream = new MemoryStream();
			df.Save(OutStream);

			OutStream.Position = 0;

			return OutStream;
		}

		private static void OutputDicomTags(IVerboseWriter writer, DicomDataset dataset, IList<DicomTag> tags,
			string header = "VALUES", short level = 1)
		{
#if VERBOSE
			// This is verbose output only, to validate
			if (writer != null)
			{
				string tabs = new string('\t', level);
				if (level > 1) header = tabs + header;
				writer.Write(header);

				// Only output values for tags to be processed
				foreach (var item in dataset.Where(ds => tags.Contains(ds.Tag)))
				{
					writer.Write($"{tabs}{item.Tag} {item.Tag.DictionaryEntry.Name}: {item.ValueRepresentation.Name} ({item.ValueRepresentation.ValueType}):");

					if (DicomVR.SQ.Code.Equals(item.ValueRepresentation.Code)
						&& dataset.TryGetSequence(item.Tag, out DicomSequence seq))
					{
						writer.Write($"{tabs}{tabs}Child datasets: {seq.Items.Count}");

						foreach (DicomDataset ChildDS in seq.Items)
						{
							// Output all child tags present
							IList<DicomTag> ChildTags = new List<DicomTag>();
							foreach (DicomItem ChildItem in ChildDS)
							{
								ChildTags.Add(ChildItem.Tag);
							}
							OutputDicomTags(writer, ChildDS, ChildTags, header: "CHILD VALUES", level: ++level);
						}
					}
					if (dataset.TryGetString(item.Tag, out string Value))
					{
						writer.Write(tabs + tabs + Value);
					}
					else
					{
						writer.Write($"{tabs}{tabs}<not a string>");
					}
				}
			}
#endif
		}

		private void AddOrUpdateDicomItem(DicomDataset dataset, DicomVR valueRepresentation, DicomTag tag, string replaceValue)
		{
			// List of ifs is a code smell, but it doesn't seem to make sense to create an interface for this
			// Also, can't use switch statement because DicomVR.CS is not a constant
			if (DicomVR.CS.Code.Equals(valueRepresentation.Code))
			{
				// TODO: If there are no special characters in the replace value, use it
				// Regex? Must be precompiled + cached
				const string ValidPattern = @"[^A-Z0-9_ ]";
				replaceValue = Regex.Replace(replaceValue, ValidPattern, "_");
				_ = dataset.AddOrUpdate(valueRepresentation, tag, replaceValue);
			}
			else if (DicomVR.DA.Code.Equals(valueRepresentation.Code))
			{
				// Strategy: Per Dr. Jeudy, add 14 days to the date
				_ = AddOrUpdateDicomItemDA(dataset, tag);
			}
			else if (DicomVR.TM.Code.Equals(valueRepresentation.Code))
			{
				// TODO: Handle TM?
			}
			else if (DicomVR.UI.Code.Equals(valueRepresentation.Code))
			{
				// TODO: How to handle hashing if needed
				_ = dataset.AddOrUpdate(valueRepresentation, tag, string.Empty);
			}
			else if (DicomVR.SQ.Code.Equals(valueRepresentation.Code))
			{
				// Look inside the sequence
				if (dataset.TryGetSequence(tag, out DicomSequence seq))
				{
					foreach (DicomDataset ChildDS in seq.Items)
					{
						// Output all child tags present
						// i.e., the list of items to process only applies at the top level
						IList<DicomItem> ToUpdate = ChildDS.ToList();
						ToUpdate
							.Each(item => AddOrUpdateDicomItem(ChildDS, item.ValueRepresentation, item.Tag, replaceValue));
					}
				}
			}
			else if (DicomVR.DS.Code.Equals(valueRepresentation.Code))
			{
				// TODO: Handle DS (Decimal String)?
			}
			else if (DicomVR.IS.Code.Equals(valueRepresentation.Code))
			{
				// TODO: Handle IS (Integer String)?
			}
			else if (DicomVR.US.Code.Equals(valueRepresentation.Code))
			{
				// Replace with 0
				_ = dataset.AddOrUpdate(valueRepresentation, tag, (ushort)0);
			}
			else if (DicomVR.OW.Code.Equals(valueRepresentation.Code))
			{
				// TODO: Handle OW (Other Word string)?
			}
			else if (DicomVR.AS.Code.Equals(valueRepresentation.Code))
			{
				// Clear age string
				_ = dataset.AddOrUpdate(valueRepresentation, tag, string.Empty);
			}
			else if (DicomVR.UN.Code.Equals(valueRepresentation.Code))
			// Can't handle "Unknown"
			{ }
			else
			{
				// Attempt to update like a string
				_ = dataset.AddOrUpdate(valueRepresentation, tag, replaceValue);
			}
		}

		private DicomDataset AddOrUpdateDicomItemDA(DicomDataset dataset, DicomTag tag)
		{
			// Convert the tag's current value to a Date
			if (dataset.TryGetString(tag, out string TagValue)
				&& DateTime.TryParseExact(TagValue, "yyyyMMdd", CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime NewValue))
			{
				return dataset.AddOrUpdate(DicomVR.DA, tag, NewValue.AddDays(14));
			}
			return null;
			// TODO: How to handle failure?
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

#if VERBOSE
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

		public string GetPatientId(Stream dicom)
		{
			if (dicom == null) throw new ArgumentNullException(nameof(dicom));

			dicom.Position = 0;
			DicomFile df = DicomFile.Open(dicom, FileReadOption.ReadAll);
			DicomTag PatientIdTag = DicomTag.Parse(PatientIdTagValue);

			if (df.Dataset.TryGetString(PatientIdTag, out string PatientId))
				return PatientId;
			else
				return null;
		}

		public Stream SetPatientId(Stream dicom, string newPatientId, IVerboseWriter writer)
		{
			if (dicom == null) throw new ArgumentNullException(nameof(dicom));
			if (string.IsNullOrWhiteSpace(newPatientId)) throw new ArgumentException(nameof(newPatientId));

			dicom.Position = 0;
			DicomFile df = DicomFile.Open(dicom, FileReadOption.ReadAll);
			DicomTag PatientIdTag = DicomTag.Parse(PatientIdTagValue);

			df.Dataset.AddOrUpdate(PatientIdTag, newPatientId);
			Stream OutStream = new MemoryStream();
			df.Dataset.Validate();
			df.Save(OutStream);

			OutputDicomTags(writer, df.Dataset, new List<DicomTag>() { PatientIdTag }, header: "AFTER SETTING ID");

			OutStream.Position = 0;

			return OutStream;
		}
	}
}
