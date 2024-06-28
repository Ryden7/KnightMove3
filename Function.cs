using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace KnightMove3;

public class Function
{
    private string accessKeyId = Environment.GetEnvironmentVariable("AccessKeyId");
    private string secretAccessKey = Environment.GetEnvironmentVariable("SecretAccessKey");
    private string bucketName = Environment.GetEnvironmentVariable("BucketName");

    private AmazonS3Client _s3Client;

    /// <summary>
    /// A simple function that takes an operationId and retrieves a KnightMove result.
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
    {
        var operationId = input.QueryStringParameters["operationId"];

        if (operationId == null || operationId == string.Empty)
        {
            return CreateResponse(200, "Please provide an operationId");
        }

        this._s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, Amazon.RegionEndpoint.GetBySystemName("us-east-1"));

        try
        {
            // Create a GetObject request
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = operationId
            };

            // Get the object from S3
            using (var response = await _s3Client.GetObjectAsync(request))
            {
                context.Logger.Log(request.BucketName);
                context.Logger.Log(request.Key);


                using (var responseStream = response.ResponseStream)
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        string responseBody = await reader.ReadToEndAsync();
                        return CreateResponse(200, responseBody);
                    }
                }

            }

        }
        catch (AmazonS3Exception e)
        {
            // Handle the exception
            context.Logger.Log($"Error encountered ***. Message:'{e.Message}' when reading object");
            return CreateResponse(400, "Not Found");
        }
        catch (Exception e)
        {
            // Handle the exception
            context.Logger.Log($"Unknown error encountered on server. Message:'{e.Message}' when reading object");
            return CreateResponse(400, "Not Found");
        }
    }

    private APIGatewayProxyResponse CreateResponse(int statusCode, string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = message,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
