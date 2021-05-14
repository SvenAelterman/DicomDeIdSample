using System;
using System.Collections.Generic;
using System.IO;
using DicomLib;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Function_01
{
    public static class DicomDeIdFx
    {
        [FunctionName("DicomDeIdFx")]
        public static void Run([BlobTrigger("dicom-samples-id/{name}", Connection = "sourceBlobConnection")]Stream inStream,
            string name,
            // Write the output to the same filename in a different container in the same storage account
            [Blob("dicom-samples-deid/{name}", FileAccess.Write, Connection = "sourceBlobConnection")]Stream outStream,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n\tName:{name}\n\tSize: {inStream.Length} Bytes");
            
            IList<string> TagKeepList = new List<string>()
            {
                "0010,0020" // Patient ID
            };

            IDicomLib lib = new FODicomWrapper();

            lib.RemoveTags(inStream, "<replaced>", TagKeepList, new VerboseLogger(log)).CopyTo(outStream);
        }
    }
}
