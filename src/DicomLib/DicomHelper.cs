using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace DicomLib
{
	public static class DicomHelper
	{
		public static string GenerateNewUid()
		{
			// Source: https://stackoverflow.com/a/14165940/816663
			// ISO/IEC 9834-8, paragraph 6.3 (referenced by DICOM PS 3.5, B.2) defines how
			// to convert a UUID to a single integer value that can be converted back into a UUID.

			const string DerivedUidRoot = "2.25.";

			// Generate a new GUID
			byte[] octets = Guid.NewGuid().ToByteArray();

			// The Guid.ToByteArray Method returns the array in a strange order (see .NET docs),
			// BigInteger expects the input array in little endian order.
			byte[] littleEndianOrder = new byte[]
			{ octets[15], octets[14], octets[13], octets[12], octets[11], octets[10], octets[9], octets[8],
				octets[6], octets[7], octets[4], octets[5], octets[0], octets[1], octets[2], octets[3],
				// The last byte controls the sign, add an additional zero to ensure
				// the array is parsed as a positive number.
				0 };

			return DerivedUidRoot + new BigInteger(littleEndianOrder).ToString(CultureInfo.InvariantCulture);
		}

		public static IList<DicomTagProcessTask> GetDefaultTags()
		{
			return new List<DicomTagProcessTask>()
			{
				new DicomTagProcessTask() { DicomTag = "0002,0000" },	// Can't add to sample
				new DicomTagProcessTask() { DicomTag = "0002,0003" },	// set in sample, UI
				new DicomTagProcessTask() { DicomTag = "0002,0016" },	// Added to sample, AE

				// TODO: Ask for clarification. Not supposed to change unless professional interpretation would change as a result of modifying the image?
				new DicomTagProcessTask() { DicomTag = "0008,0018", ProcessAction = DicomTagProcessAction.Ignore },	// set in sample, UI

				new DicomTagProcessTask() { DicomTag = "0008,0020" },
				new DicomTagProcessTask() { DicomTag = "0008,0021" },
				new DicomTagProcessTask() { DicomTag = "0008,0022" },	// Added 2021-11-29
				new DicomTagProcessTask() { DicomTag = "0008,0023" },
				new DicomTagProcessTask() { DicomTag = "0008,0030" },
				new DicomTagProcessTask() { DicomTag = "0008,0031", ProcessAction = DicomTagProcessAction.Ignore },
				new DicomTagProcessTask() { DicomTag = "0008,0032", ProcessAction = DicomTagProcessAction.Ignore },
				new DicomTagProcessTask() { DicomTag = "0008,0033", ProcessAction = DicomTagProcessAction.Ignore },
				new DicomTagProcessTask() { DicomTag = "0008,0050" },	// Added to sample, SH
				new DicomTagProcessTask() { DicomTag = "0008,0080" },	// Added to sample, LO
				new DicomTagProcessTask() { DicomTag = "0008,0081" },	// Added to sample, ST
				new DicomTagProcessTask() { DicomTag = "0008,0090" },	// Added to sample, PN
				new DicomTagProcessTask() { DicomTag = "0008,0102" },	// Added 2021-11-29
				new DicomTagProcessTask() { DicomTag = "0008,1010" },	// Added to sample, SH
				new DicomTagProcessTask() { DicomTag = "0008,1032" },
				new DicomTagProcessTask() { DicomTag = "0008,1070" },	// Added to sample, PN
				new DicomTagProcessTask() { DicomTag = "0008,1048" },
				new DicomTagProcessTask() { DicomTag = "0008,1150", ProcessAction = DicomTagProcessAction.Redact },	// child of SQ 8,1140
				new DicomTagProcessTask() { DicomTag = "0008,1155", ProcessAction = DicomTagProcessAction.Redact },	// child of SQ 8,1140

				new DicomTagProcessTask() { DicomTag = "0010,0010" },	// Updated in sample, PN
				new DicomTagProcessTask() { DicomTag = "0010,0020" },	// Added to sample, LO
				new DicomTagProcessTask() { DicomTag = "0010,0030" },	// Added to sample, DA
				new DicomTagProcessTask() { DicomTag = "0010,1001" },	// Added 2021-11-29
				new DicomTagProcessTask() { DicomTag = "0010,1010" },	// Added to sample, AS
				new DicomTagProcessTask() { DicomTag = "0010,21C0" },	// Added to sample, US 0001

				new DicomTagProcessTask() { DicomTag = "0018,1000" },	// Add to sample, LO (retired tag, per LEADTOOLS)

				new DicomTagProcessTask() { DicomTag = "0020,0010" },
				// Tags that need to be redacted
				new DicomTagProcessTask() { DicomTag = "0020,000D", ProcessAction = DicomTagProcessAction.Redact },
				new DicomTagProcessTask() { DicomTag = "0020,000E", ProcessAction = DicomTagProcessAction.Redact },

				new DicomTagProcessTask() { DicomTag = "0020,0052" },

				new DicomTagProcessTask() { DicomTag = "0029,1009" },
				new DicomTagProcessTask() { DicomTag = "0029,1019" },

				new DicomTagProcessTask() { DicomTag = "0032,1032" },
				new DicomTagProcessTask() { DicomTag = "0032,1064" },

				new DicomTagProcessTask() { DicomTag = "0040,0007" },	// Added to sample, LO
				new DicomTagProcessTask() { DicomTag = "0040,0244" },
				new DicomTagProcessTask() { DicomTag = "0040,0245" },
				new DicomTagProcessTask() { DicomTag = "0040,0253" },
				new DicomTagProcessTask() { DicomTag = "0040,0275" },	// Unable to add to sample
				// TODO: Confirm? This does not seem like PHI?
				new DicomTagProcessTask() { DicomTag = "0040,1001", ProcessAction = DicomTagProcessAction.Ignore },
			};
		}
	}
}
