using DicomLib;
using System;
using System.IO;
using System.Reflection;

namespace LocalClient
{
	internal class Program
	{
		private static StreamWriter _LogFile;

		/// <summary>
		/// Main execution.
		/// </summary>
		/// <param name="args">Required argument is the ID map CSV file.</param>
		private static int Main(string[] args)
		{
			string IdMapCsvPath;
			string DicomSourcePath;
			// Assume the CSV path is the first argument and the path to the DICOM source files is the second argument
			int IdMapCsvArgIndex = 0,
				DicomSourceArgIndex = 1;

			if (args.Length < 2)
			{
				WriteLine("Please specify the path and name of the ID map CSV file and the folder containing the DICOM files as parameters.", MessageType.Error);
				return -1;
			}

			IdMapCsvPath = args[IdMapCsvArgIndex];
			DicomSourcePath = args[DicomSourceArgIndex];

			// Validate that the CSV file and the DICOM source path exist
			if (!File.Exists(IdMapCsvPath))
			{
				WriteLine($"The path '{IdMapCsvPath}' does not refer to an existing file.", MessageType.Error);
				return -2;
			}
			else
			{
				WriteLine($"Found ID map at '{IdMapCsvPath}'");
			}

			EnumerationOptions eo = new EnumerationOptions() { RecurseSubdirectories = true };

			string FullSourcePath = Path.GetFullPath(DicomSourcePath);
			if (!Path.EndsInDirectorySeparator(DicomSourcePath))
				FullSourcePath = Path.Join(FullSourcePath, Path.DirectorySeparatorChar.ToString());

			if (!Directory.Exists(FullSourcePath))
			{
				WriteLine($"The path '{FullSourcePath}' does not refer to an existing folder.", MessageType.Error);
				return -4;
			}
			else
			{
				string[] DicomSourceFiles = Directory.GetFiles(FullSourcePath, "*.dcm", eo);

				if (DicomSourceFiles.Length > 0)
				{
					WriteLine($"Found {DicomSourceFiles.Length} DICOM source file(s).");
				}
				else
				{
					WriteLine($"The path '{FullSourcePath}' does not contain any .dcm files.", MessageType.Error);
				}
			}

			string UidMapCsvPath = Path.Join(FullSourcePath, CsvUidMapProvider.DefaultFileName);

			if (!File.Exists(UidMapCsvPath))
			{
				WriteLine($"There is no UID map CSV file found at '{UidMapCsvPath}'; one will be created.", MessageType.Warning);
			}
			else
			{
				WriteLine($"Found UID map CSV file at '{UidMapCsvPath}'");
			}

			string LogFileName = Path.Join(FullSourcePath, "DicomProcess.log");
			WriteLine($"Using Log File '{LogFileName}'.");
			InitializeLogFile(LogFileName);

			IIdMapProvider idmap = new CsvIdMapProvider(IdMapCsvPath);

			WriteLine($"ID map contains {idmap.IdMap.Count} entries.");

			IUidMapProvider uidmap = new CsvUidMapProvider(UidMapCsvPath);
			ISourceContainerProvider sourceFiles = new FileSystemSourceContainerProvider(FullSourcePath);

			string OutputDirectoryName = $"{new DirectoryInfo(FullSourcePath).Name}-Output";
			ITargetContainerProvider target = new FileSystemTargetContainerProvider(OutputDirectoryName);

			ProcessDicomFiles(idmap, uidmap, sourceFiles, target);

			WriteLine("Done!");
			_LogFile.Close();

			return 0;
		}

		private static void InitializeLogFile(string logFileName)
		{
			_LogFile = new StreamWriter(new FileStream(logFileName, FileMode.Append, FileAccess.Write));

			_LogFile.WriteLine($"# Date: {DateTime.UtcNow:u}");
			_LogFile.WriteLine("# Version: 1.0");
			_LogFile.WriteLine($"# Software: DICOM LocalClient {Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
		}

		private static void ProcessDicomFiles(IIdMapProvider idMapProvider, IUidMapProvider uidMapProvider,
			ISourceContainerProvider sourceContainerProvider, ITargetContainerProvider target)
		{
			DicomSourceFile CurrentDicom;
			//IList<DicomTagProcessTask> TagProcessList = DicomHelper.GetDefaultTags();

			IDicomLib lib = new FODicomWrapper(uidMapProvider, CsvIdMapProvider.DefaultPartitionKey, null);

			while (!sourceContainerProvider.AtEnd)
			{
				CurrentDicom = sourceContainerProvider.ReadNext();
				WriteLine($"Processing DICOM file '{CurrentDicom.FileName}'");

				string CurrentPatientId = lib.GetPatientId(CurrentDicom.Contents);

				try
				{
					string NewPatientId = idMapProvider.GetStudyId(CsvIdMapProvider.DefaultPartitionKey, CurrentPatientId);
					using Stream ModifiedContents = lib.SetPatientId(CurrentDicom.Contents, NewPatientId, null);

					target.Write(CurrentDicom.FileName, ModifiedContents);
				}
				catch (WrappedDicomValidationException ex)
				{
					string Message = $"Unable to set Study ID: '{ex.Message}'";
					WriteLine(Message, MessageType.Error);
					_LogFile.WriteLine($"{DateTime.UtcNow:u}\t[ERROR]\t[File: '{CurrentDicom.FileName}']\t{Message}");
				}
				catch (MissingPatientIdInIdMapException ex)
				{
					string Message = $"Unable to get Study ID: '{ex.Message}'";
					WriteLine(Message, MessageType.Error);
					_LogFile.WriteLine($"{DateTime.UtcNow:u}\t[ERROR]\t[File: '{CurrentDicom.FileName}']\t{Message}");
				}
			}
		}

		private static void WriteLine(string value, MessageType messageType = MessageType.Info)
		{
			ConsoleColor CurrentFgColor = Console.ForegroundColor;

			try
			{
				switch (messageType)
				{
					case MessageType.Warning:
						Console.ForegroundColor = ConsoleColor.Yellow;
						break;
					case MessageType.Error:
						Console.ForegroundColor = ConsoleColor.Red;
						break;
				}

				Console.WriteLine(value);
			}
			finally
			{
				Console.ForegroundColor = CurrentFgColor;
			}
		}
	}

	internal enum MessageType
	{
		Info,
		Warning,
		Error
	}
}
