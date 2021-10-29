using System.IO;

namespace DicomLib
{
	public class DicomSourceFile
	{
		public DicomSourceFile(string fileName, Stream contents)
		{
			FileName = fileName;
			Contents = contents;
		}

		public string FileName { get; }
		public Stream Contents { get; }
	}
}
