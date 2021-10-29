namespace DicomLib
{
	public interface IIdMapProvider
	{
		string GetStudyId(string institutionId, string currentPatientID);
	}
}
