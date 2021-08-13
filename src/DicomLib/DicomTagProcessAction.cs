namespace DicomLib
{
	public enum DicomTagProcessAction
	{
		/*
		 * http://dicom.nema.org/medical/dicom/current/output/chtml/part15/chapter_E.html#table_E.1-1a
		 * Single letter comments refer to the rough equivalent of the action codes found in the table above.
		 */
		Ignore = 0,     // Do nothing
		Redact = 1,     // U
		Clear = 2,      // X
		Sequence = 3
	}
}