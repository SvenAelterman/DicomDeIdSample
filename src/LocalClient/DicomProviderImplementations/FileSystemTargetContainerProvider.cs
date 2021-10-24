using DicomLib;
using System.IO;

namespace LocalClient
{
	internal class FileSystemTargetContainerProvider : ITargetContainerProvider
	{
		private readonly string _outputPath;

		public FileSystemTargetContainerProvider(string outputPath)
		{
			_outputPath = outputPath;
		}

		public void Write(string fileName, Stream contents)
		{
			// Create the full file path
			string filePath = Path.Join(_outputPath, fileName);

			// Ensure the output path exists
			Directory.CreateDirectory(Directory.GetParent(filePath).FullName);

			// TODO: Persist
			FileStream fs = new FileStream(filePath, FileMode.Create);
			contents.CopyTo(fs);
			fs.Close();
		}
	}
}