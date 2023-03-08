using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ML.Dynamic;
using ML.Helpers;
using Newtonsoft.Json;

namespace ML.AzureFunction
{
    public class Function1
    {
        private readonly ILogger _logger;

        private readonly IMLPredictor predictor;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();

            var pathToMLModel = "Resources/Models/JuliaMLModel_Reg.zip";
            predictor = new MLPredictor(pathToMLModel);
        }

        [Function("Function1")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            float? result = null;
            var body = req.ReadFromJsonAsync<Body>().Result;
            if (body == null)
            {
                _logger.LogError($"Could not deserialize body: '{req.ReadAsString()}'");
            }
            else
            {
                result = predictor.Predict(body.ColumnInfo, body.Parameters).Result;
            }

            response.WriteString($"{result}");

            return response;
        }
    }

    internal class Body
    {
        public ColumnInfo ColumnInfo { get; set; } = new();
        public Dictionary<string, object?> Parameters { get; set; } = new();
    }

    internal class MLPredictor : IMLPredictor
    {
        private readonly string localModelPath;

        public MLPredictor(string localModelPath)
        {
            this.localModelPath = localModelPath;
        }
        public Task<float?> Predict(ColumnInfo colInfo, Dictionary<string, object?> parameters)
        {
            var prediction = MLDynamicPredict.PredictFromModel(localModelPath, colInfo, parameters);
            return Task.FromResult((float?)prediction);
        }
    }

}
