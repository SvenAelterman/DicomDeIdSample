using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Function_01
{
    public static class DicomDeIdFx
    {
        [FunctionName("DicomDeIdFx")]
        public static void Run([BlobTrigger("DicomSamplesId/{name}", Connection = "sourceBlobConnection")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
