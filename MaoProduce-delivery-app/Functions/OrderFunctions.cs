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
    public class OrderFunctions
    {
        /// <summary>
        /// This const is the name of the environment variable that the serverless.template will use to set
        /// </summary>
        const string TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP = "OrderTable";

        public const string ID_QUERY_STRING_NAME = "CustomerId";
        IDynamoDBContext DDBContext { get; set; }

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public OrderFunctions()
        {
            // Check to see if a table name was passed in through environment variables and if so
            // add the table mapping.adfasdfasdf
            var tableName = System.Environment.GetEnvironmentVariable(TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP);
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(CustomerOrders)] = new Amazon.Util.TypeMapping(typeof(CustomerOrders), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(new AmazonDynamoDBClient(), config);
        }

        /// <summary>
        /// Constructor used for testing passing in a preconfigured DynamoDB client.
        /// </summary>
        /// <param name="ddbClient"></param>
        /// <param name="tableName"></param>
        public OrderFunctions(IAmazonDynamoDB ddbClient, string tableName)
        {
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(CustomerOrders)] = new Amazon.Util.TypeMapping(typeof(CustomerOrders), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(ddbClient, config);
        }

        /// <summary>
        /// A Lambda function that returns a list of orders. Open orders on default.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of blogs</returns>
        public async Task<APIGatewayProxyResponse> GetOrdersAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Getting orders");
            var search = this.DDBContext.ScanAsync<CustomerOrders>(null);
            var page = await search.GetNextSetAsync();
            List<Orders> orderList = new List<Orders>();

            foreach(var customer in page)
            {
                foreach(var list in customer.Orders)
                {
                    if (list.IsOpen == true)
                    {
                        //if open filter
                        orderList.Add(list);
                    }
                    else
                    {
                        //if all filter
                        orderList.Add(list);
                    }
                }

            }



            context.Logger.LogLine($"Found {page.Count} orders");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = LowercaseJsonSerializer.SerializeObject(orderList),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            return response;
        }

        /// <summary>
        /// A Lambda function that returns orders from a single customer.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> GetOrdersByCustAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            //Two parameters should be passed. Customer Id is required to load orders from customer
            bool isOpen = true;
            string customerId = null;
            List<Orders> orderList = new List<Orders>();

            //check for customerId parameter
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.QueryStringParameters[ID_QUERY_STRING_NAME];
            //check for status(isOpen) filter
            if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("isOpen"))
                isOpen = bool.Parse(request.QueryStringParameters["isOpen"]);

            //Check if CustomerId exist; else send bad request response.
            if (string.IsNullOrEmpty(customerId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            //Function to load from dynamodb
            context.Logger.LogLine($"Getting orders from customer {customerId}");
            var orders = await DDBContext.LoadAsync<CustomerOrders>(customerId);
            context.Logger.LogLine($"Found Orders for Customer: {orders != null}");

            //check if orders exist in customer
            if (orders == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string>{ { "message", "The cutomer orders is empty." } }),
                    Headers = new Dictionary <string, string> { { "Content-Type", "application/json"} }
                };
            }

            //check if filter is requested else pass all orders from customer
            if (isOpen)
            {
                foreach (var item in orders.Orders)
                {
                    if (item.IsOpen == true)
                    {
                        orderList.Add(item);
                    }
                }
            }
            else
            {
                orderList = orders.Orders;
            }
           
            //response
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = LowercaseJsonSerializer.SerializeObject(orderList),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that adds a blog post.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> AddOrderAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var order = JsonConvert.DeserializeObject<Orders>(request?.Body);
            order.Id = Guid.NewGuid().ToString();
            order.DateTime = DateTime.Now;

            context.Logger.LogLine($"Saving orderomer details with id {order.Id}");
            await DDBContext.SaveAsync<Orders>(order);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = order.Id.ToString(),
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that removes a blog post from the DynamoDB table.
        /// </summary>
        /// <param name="request"></param>
        public async Task<APIGatewayProxyResponse> RemoveOrderAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string orderId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))

                orderId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                orderId = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            if (string.IsNullOrEmpty(orderId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            context.Logger.LogLine($"Deleting blog with id {orderId}");
            await this.DDBContext.DeleteAsync<Orders>(orderId);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }
    }
}
