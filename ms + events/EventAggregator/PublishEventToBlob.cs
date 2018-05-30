// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGridExtensionConfig?functionName={functionname}

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.IO;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;

namespace EventRecorder
{
    public static class PublishEventToBlob
    {
        [FunctionName("PublishEventToBlob")]
        public static async Task Run([EventGridTrigger] string eventJson,
            [Blob("allevents", FileAccess.Read)] CloudBlobContainer container,
            TraceWriter log)
        {
            log.Info(eventJson);

            var evt = (JObject)JsonConvert.DeserializeObject(eventJson);

            var id = evt["id"].ToString();

            var blob = container.GetBlockBlobReference(id);

            await blob.UploadTextAsync(eventJson);
        }
    }
}
