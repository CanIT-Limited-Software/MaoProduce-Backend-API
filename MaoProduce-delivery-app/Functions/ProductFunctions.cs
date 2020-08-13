using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
//[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MaoProduce_delivery_app
{
    public class ProductFunctions
    {
        // This const is the name of the environment variable that the serverless.template will use to set
        // the name of the DynamoDB table used to store blog posts.
        const string TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP = "ProductTable";

        public const string ID_QUERY_STRING_NAME = "Id";
        IDynamoDBContext DDBContext { get; set; }

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public ProductFunctions()
        {
            // Check to see if a table name was passed in through environment variables and if so
            // add the table mapping.
            var tableName = System.Environment.GetEnvironmentVariable(TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP);
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Products)] = new Amazon.Util.TypeMapping(typeof(Products), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(new AmazonDynamoDBClient(), config);
        }

        /// <summary>
        /// Constructor used for testing passing in a preconfigured DynamoDB client.
        /// </summary>
        /// <param name="ddbClient"></param>
        /// <param name="tableName"></param>
        public ProductFunctions(IAmazonDynamoDB ddbClient, string tableName)
        {
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Products)] = new Amazon.Util.TypeMapping(typeof(Products), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(ddbClient, config);
        }

        /// <summary>
        /// A Lambda function that returns back a product and details
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of blogs</returns>
        public async Task<APIGatewayProxyResponse> GetProductsAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Getting products");
            var search = this.DDBContext.ScanAsync<Products>(null);
            var page = await search.GetNextSetAsync();


            context.Logger.LogLine($"Found {page.Count} products");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = LowercaseJsonSerializer.SerializeObject(page),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            };

            return response;
        }

        /// <summary>
        /// A Lambda function that returns the product identified by Id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> GetProductAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string Id = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                Id = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                Id = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            if (string.IsNullOrEmpty(Id))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            context.Logger.LogLine($"Getting single product {Id}");
            var product = await DDBContext.LoadAsync<Products>(Id);
            context.Logger.LogLine($"Found product: {product != null}");

            if (product == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(product),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that adds a product post.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> AddProductAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var product = JsonConvert.DeserializeObject<Products>(request?.Body);
            product.Id = Guid.NewGuid().ToString();

            context.Logger.LogLine($"Saving productomer details with id {product.Id}");
            try
            {
                await DDBContext.SaveAsync<Products>(product);

                var body = new Dictionary<string, string>
                {
                    { "message", "Sucessfully created a new user!"},
                    { "userId", product.Id.ToString() }
                };

                var response = new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(body),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };
                return response;
            } catch (Exception e)
            {
                var response = new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Body = e.ToString(),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };
                return response;
            }
            
        }

        /// <summary>
        /// A Lambda function that removes a product post from the DynamoDB table.
        /// </summary>
        /// <param name="request"></param>
        public async Task<APIGatewayProxyResponse> RemoveProductAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string Id = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))

                Id = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                Id = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            if (string.IsNullOrEmpty(Id))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            context.Logger.LogLine($"Deleting product with id {Id}");
            await this.DDBContext.DeleteAsync<Products>(Id);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "message", "Sucessfully deleted the user"}, { "userId", Id.ToString()} }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            };
        }


        /// <summary>
        /// A Lambda function that updates product details post from the DynamoDB table.
        /// </summary>
        /// <param name="request"></param>
        public async Task<APIGatewayProxyResponse> UpdateProductAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string productId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                productId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                productId = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            if (string.IsNullOrEmpty(productId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }


            var product = JsonConvert.DeserializeObject<Products>(request?.Body);
            product.Id = productId;

            context.Logger.LogLine($"Saving product details with id {product.Id}");
            await DDBContext.SaveAsync<Products>(product);

            //Set the body in json form
            Dictionary<string, string> body = new Dictionary<string, string>
            {
                { "message", "Product data have been updated!" },
                { "UserId", product.Id.ToString() }
            };

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(body),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            };
            return response;
        }
    }
}
