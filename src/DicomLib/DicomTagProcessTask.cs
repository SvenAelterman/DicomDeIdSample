namespace DicomLib
{
	public class DicomTagProcessTask
	{
		public string DicomTag { get; set; }
		public DicomTagProcessAction ProcessAction { get; set; } = DicomTagProcessAction.Clear;
	}
}
