using DicomLib;
using System;
using System.IO;

namespace LocalClient
{
	internal class FileSystemSourceContainerProvider : ISourceContainerProvider
	{
		private string _path;
		private bool _pathIsDirectory = false;
		private int _counter = 0;
		private string[] _files;

		public bool AtEnd { get { return !_pathIsDirectory || _counter >= _files.Length; } }

		/// <summary>
		/// Creates a new instance of the <see cref="FileSystemSourceContainerProvider"/> class.
		/// </summary>
		/// <param name="path">Either the path to a folder for reading multiple DICOM files, or a single DICOM file to read or write.</param>
		public FileSystemSourceContainerProvider(string path)
		{
			_path = path;

			if (Directory.Exists(path))
			{
				_pathIsDirectory = true;
				_files = Directory.GetFiles(path, "*.dcm",
					new EnumerationOptions() { RecurseSubdirectories = true });
			}
		}

		public DicomSourceFile ReadNext()
		{
			if (!AtEnd)
			{
				if (!_pathIsDirectory)
				{
					// Read the specified file
					return ReadFile(_path);
				}
				else
				{
					// Read the next file
					var f = ReadFile(_files[_counter]);
					_counter++;
					return f;
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		private DicomSourceFile ReadFile(string filePath)
		{
			return new DicomSourceFile(
						Path.GetFileName(filePath),
						new MemoryStream(File.ReadAllBytes(filePath)));
		}
	}
}
