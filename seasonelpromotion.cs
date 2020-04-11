using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace async_http_api
{
    public static class seasonelpromotion
    {
        [FunctionName("seasonelpromotion_HttpStart")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            string instanceId = await starter.StartNewAsync("seasonelpromotion", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            string checkStatusLocacion = string.Format("{0}://{1}/api/status/{2}", req.Scheme, req.Host, instanceId);
            string message = $"Status update of the promotional items : {checkStatusLocacion}";

            ActionResult response = new AcceptedResult(checkStatusLocacion, message);
            req.HttpContext.Response.Headers.Add("retry-after", "20");
            return response;
        }

        [FunctionName("seasonelpromotion")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            outputs.Add(await context.CallActivityAsync<string>("applypromotions", "Watch Collection"));
            return outputs;
        }

        [FunctionName("applypromotions")]
        public static string applypromotions([ActivityTrigger] string products, ILogger log)
        {
            log.LogInformation($"Promotional Items {products}.");
            return $"Promotional Items {products}!";
        }

        [FunctionName("GetStatus")]
        public static async Task<IActionResult> Run(
         [HttpTrigger(AuthorizationLevel.Anonymous, methods: "get", Route = "status/{instanceId}")] HttpRequest req,
         [DurableClient] IDurableOrchestrationClient orchestrationClient, string instanceId, ILogger logger)
        {
            var status = await orchestrationClient.GetStatusAsync(instanceId);
            if (status != null)
            {
                if (status.RuntimeStatus == OrchestrationRuntimeStatus.Running || status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
                {
                    string checkStatusLocacion = string.Format("{0}://{1}/api/status/{2}", req.Scheme, req.Host, instanceId);
                    string message = $"The current status is {status.RuntimeStatus}. Check status later : GET {checkStatusLocacion}"; 

                    ActionResult response = new AcceptedResult(checkStatusLocacion, message); 
                    req.HttpContext.Response.Headers.Add("retry-after", "20");  
                    return response;
                }
                else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                    return new OkObjectResult($"The update process is completed with '{instanceId}', Function output '{status.Output}'");
            }
            return new NotFoundObjectResult($"Instance Id - '{instanceId}' not found.");
        }
    }
}