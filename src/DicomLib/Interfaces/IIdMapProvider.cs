using System.Collections.Generic;

namespace DicomLib
{
	public interface IIdMapProvider
	{
		string GetStudyId(string institutionId, string currentPatientID);

		IList<IdMapEntity> IdMap { get; }
	}
}
