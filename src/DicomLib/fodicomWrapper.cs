using Azure;
using Azure.AI.TextAnalytics;
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
		public Stream RemoveTags(Stream dicom, string replaceValue, IList<string> keepTags,
			IVerboseWriter writer)
		{
			// TODO: This is very inefficient... if an image came from the same patient and the same scan, would the metadata not be the same? Why evaluate it every time?
			// TODO: Cache results?

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

			// This is verbose output only, to validate
			OutputTags(writer, df);

			List<DicomTag> ToUpdate = new List<DicomTag>();
			//List<DicomTag> ToTextAnalyzer = new List<DicomTag>();
			List<TextDocumentInput> TextAnalysisDocuments = new List<TextDocumentInput>();

			// Create the list of tags to be updated/replaced/cleared that are "person names"
			// Deliberately don't include other tags for this sample
			//foreach (var fmi in df.Dataset.Where(ds => ds.ValueRepresentation.Code == "PN"))
			foreach (var fmi in df.Dataset.Where(ds => !Keep.Contains(ds.Tag)))
			{

				//if (!Keep.Contains(fmi.Tag))
				//{
				// TODO: How to limit the number of tags to send to TextAnalyzer?
				//ToTextAnalyzer.Add(fmi.Tag);
				if (df.Dataset.TryGetString(fmi.Tag, out string value) &&
					!string.IsNullOrWhiteSpace(value))
				{
					// TODO: Max 5 records...
					TextAnalysisDocuments.Add(new TextDocumentInput(fmi.Tag.ToString(), value));

					if (TextAnalysisDocuments.Count > 4) break;
				}
				//ToUpdate.Add(fmi.Tag);
				//}
			}

			// TODO: svaelter: Make an app setting
			Uri EndpointUri = new Uri("https://ta-dicomdeid-demo-eastus2-01.cognitiveservices.azure.com/");

			TextAnalyticsClientOptions opt = new TextAnalyticsClientOptions(TextAnalyticsClientOptions.ServiceVersion.V3_1_Preview_5)
			{
				DefaultCountryHint = "us",
				DefaultLanguage = "en"
			};

			//TextAnalyticsClient client = new TextAnalyticsClient(EndpointUri, new DefaultAzureCredential(), opt);
			TextAnalyticsClient client = new TextAnalyticsClient(EndpointUri, new AzureKeyCredential("b3480fb81cbf4b84abb2d162d1a2f602"), opt);

			Azure.Response<RecognizePiiEntitiesResultCollection> r = client.RecognizePiiEntitiesBatch(TextAnalysisDocuments);

			// TODO: Determine if tags need updating based on identified PII

			// Process each tag
			foreach (var tag in ToUpdate)
			{
				// TODO: Log original value, etc.
				df.Dataset.AddOrUpdate(new DicomPersonName(tag, replaceValue));
			}

			df.Dataset.Validate();

			// This is verbose output only, to validate
			OutputTags(writer, df);

			Stream OutStream = new MemoryStream();
			df.Save(OutStream);

			OutStream.Position = 0;

			return OutStream;
		}

		private void OutputTags(IVerboseWriter writer, DicomFile df)
		{
			if (writer == null) return;

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
	}
}
