using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AzureSkyMedia.FunctionApp
{
    //public static partial class MediaWorkflow
    //{
    //    [FunctionName("MediaWorkflow-CosmosData")]
    //    public static void Run([CosmosDBTrigger(
    //        databaseName: "databaseName",
    //        collectionName: "collectionName",
    //        ConnectionStringSetting = "",
    //        LeaseCollectionName = "leases")]IReadOnlyList<Document> input, ILogger log)
    //    {
    //        if (input != null && input.Count > 0)
    //        {
    //            log.LogInformation("Documents modified " + input.Count);
    //            log.LogInformation("First document Id " + input[0].Id);
    //        }
    //    }
    //}
}
