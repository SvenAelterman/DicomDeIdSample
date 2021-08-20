using Dicom;

namespace DicomLib
{
	[System.Diagnostics.DebuggerDisplay("{Tag}, {ProcessAction}")]
	internal class fodicomTask
	{
		public DicomTag Tag { get; set; }
		public DicomTagProcessAction ProcessAction { get; set; }
	}
}
