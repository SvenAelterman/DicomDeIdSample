using Microsoft.Azure.Cosmos.Table;

namespace DicomLib
{
	public class IdMapEntity : TableEntity
	{
		// Partition Key is the institution ID (3 chars)
		// Row Key is the subject's MRN

		public string StudyId { get; set; }
	}
}
