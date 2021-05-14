using System;
using System.Collections.Generic;
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
            IList<string> TagKeepList = new List<string>()
            {
                "0002,0000"
            };

            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
