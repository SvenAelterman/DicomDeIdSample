using System.Collections.Generic;
using System.IO;

namespace DicomLib
{
	public interface IDicomLib
	{
		/// <summary>
		/// Replaces tag values with the specified value, except
		/// if the tag matches a value specified in the keepTags list.
		/// </summary>
		/// <param name="dicom"></param>
		/// <param name="replaceValue"></param>
		/// <param name="keepTags">(optional)</param>
		/// <param name="writer"></param>
		/// <returns>A new Stream object containing the new DicomFile</returns>
		Stream RemoveTags(Stream dicom, string replaceValue, IList<string> keepTags, IVerboseWriter writer);

		Stream ProcessTags(Stream dicom, string replaceValue, IList<string> tagsToProcess, IVerboseWriter writer);
		string GetPatientId(Stream dicom);
		Stream SetPatientId(Stream dicom, string newPatientId, IVerboseWriter writer);
	}
}
