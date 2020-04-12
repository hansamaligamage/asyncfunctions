# Async functions in durable function framework
This sample code has showed how to handle the asynchronous fucntion calls in durable function framework. The code is developed on .NET Core 3.1 and function v3 in Visual Studio 2019

## Installed Packages
Microsoft.NET.Sdk.Functions version 3 (3.0.5) and Microsoft.Azure.WebJobs.Extensions.DurableTask 2 (2.2.0)

## Code snippets
### Http trigger function
This is a http trigger function and the entry point for the application
```
       [FunctionName("seasonelpromotion_HttpStart")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            string instanceId = await starter.StartNewAsync("seasonelpromotion", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            string checkStatusLocacion = string.Format("{0}://{1}/api/status/{2}", req.Scheme, req.Host, 
              instanceId);
            string message = $"Status update of the promotional items : {checkStatusLocacion}";

            ActionResult response = new AcceptedResult(checkStatusLocacion, message);
            req.HttpContext.Response.Headers.Add("retry-after", "20");
            return response;
        }
```

### Orchestrator function to call the activity function
```
        [FunctionName("seasonelpromotion")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            outputs.Add(await context.CallActivityAsync<string>("applypromotions", "Watch Collection"));
            return outputs;
        }
```
  
  ### Activity function
  Let's assume this function takes long time to process,
  ```
        [FunctionName("applypromotions")]
        public static string applypromotions([ActivityTrigger] string products, ILogger log)
        {
            log.LogInformation($"Promotional Items {products}.");
            return $"Promotional Items {products}!";
        }
  ```

### Status check function for the long running process
```
        [FunctionName("GetStatus")]
        public static async Task<IActionResult> Run(
         [HttpTrigger(AuthorizationLevel.Anonymous, methods: "get", 
                        Route = "status/{instanceId}")] HttpRequest req,
         [DurableClient] IDurableOrchestrationClient orchestrationClient, string instanceId, ILogger logger)
        {
            var status = await orchestrationClient.GetStatusAsync(instanceId);
            if (status != null)
            {
                if (status.RuntimeStatus == OrchestrationRuntimeStatus.Running 
                    || status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
                {
                    string checkStatusLocacion = string.Format("{0}://{1}/api/status/{2}", req.Scheme, 
                                                  req.Host, instanceId);
                    string message = $"The current status is {status.RuntimeStatus}. 
                                      Check status later : GET {checkStatusLocacion}"; 

                    ActionResult response = new AcceptedResult(checkStatusLocacion, message); 
                    req.HttpContext.Response.Headers.Add("retry-after", "20");  
                    return response;
                }
                else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                    return new OkObjectResult($"The update process is completed with '{instanceId}', 
                      Function output '{status.Output}'");
            }
            return new NotFoundObjectResult($"Instance Id - '{instanceId}' not found.");
        }
```
