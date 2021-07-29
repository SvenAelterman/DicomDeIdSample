using Microsoft.Azure.Cosmos.Table;

namespace DicomLib
{
	public class IdMapEntity : TableEntity
	{
		public string StudyId { get; set; }
	}
}
