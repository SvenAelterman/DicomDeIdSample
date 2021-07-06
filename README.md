# DICOM De-Identification Sample

A sample Azure Function that de-identifies DICOM files by removing tags from new blobs added to Azure Storage.

This sample uses the [fo-dicom](https://github.com/fo-dicom/fo-dicom) .NET library to process the DICOM files.

## Running Locally
Pre-reqs: The Azure Storage Emulator

1. Open the solution in Visual Studio (2019 or later).
2. Ensure the startup project is set to `Function-01`. 
3. Start the VS debugger for the function project.
4. Using Storage Explorer, send one or more DICOM files with tags to the source container identified in the code: `dicom-samples-id`.
5. You should observe output from the Verbose logger in the VS debug window:
