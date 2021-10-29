namespace DicomLib
{
	/// <summary>
	/// 'Container' refers to the generic concept of a bucket to hold DICOM files.
	/// This could be a directory on a Windows or Linux file system, or an Azure Storage container, etc.
	/// </summary>
	public interface ISourceContainerProvider
	{
		DicomSourceFile ReadNext();

		bool AtEnd { get; }
	}
}
