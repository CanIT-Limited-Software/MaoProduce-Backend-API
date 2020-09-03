using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MaoProduce_delivery_app.Functions;
using MaoProduce_delivery_app.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

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
            // add the table mapping.
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


        ///<SUMMMARY>
        /// A Function that will load customer database
        ///</---->
        private void LoadDatabase()
        {
            var tableName = System.Environment.GetEnvironmentVariable("CustomerTable");
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Customers)] = new Amazon.Util.TypeMapping(typeof(Customers), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(new AmazonDynamoDBClient(), config);
        }

        /// <summary>
        /// A Lambda function that returns a list of orders. Open orders on default.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of orders</returns>
        public async Task<APIGatewayProxyResponse> GetOrdersAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var search = this.DDBContext.ScanAsync<CustomerOrders>(null);
            var page = await search.GetNextSetAsync();

            //Create a list that will store orders with extra fields cust id and name
            List<Order_AllOrders> orderList = new List<Order_AllOrders>();
            bool isOpen = true;

            //Check for filters
            //check for status(isOpen) filter
            if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("isOpen"))
                isOpen = bool.Parse(request.QueryStringParameters["isOpen"]);


            //Cycle through users and get the orders
            foreach (var customer in page)
            {
                foreach (var item in customer.Orders)
                {
                    //instantiate all order object to convert orders later
                    var allorder = new Order_AllOrders();
                    if (isOpen)
                    {
                        //call function to get customer from customer table
                        LoadDatabase();
                        //call dynamodb 
                        var cust = await DDBContext.LoadAsync<Customers>(customer.CustomerId);
                        if (cust == null)
                            continue;

                        //assign all fields appropriately
                        allorder.CustomerId = customer.CustomerId;
                        allorder.CustomerName = cust.Name;
                        allorder.DateTime = item.DateTime;
                        allorder.Id = item.Id;
                        allorder.IsOpen = item.IsOpen;
                        allorder.Products = item.Products;
                        allorder.Signature = item.Signature;
                        allorder.TotalPrice = item.TotalPrice;

                        //check status
                        if (item.IsOpen == true)
                            orderList.Add(allorder);
                    }
                    else
                    {
                        //call function to get customer from customer table
                        LoadDatabase();
                        //call dynamodb 
                        var cust = await DDBContext.LoadAsync<Customers>(customer.CustomerId);

                        //convert order to allorders -> assign all fields appropriately
                        allorder.CustomerId = customer.CustomerId;
                        allorder.CustomerName = cust.Name;
                        allorder.DateTime = item.DateTime;
                        allorder.Id = item.Id;
                        allorder.IsOpen = item.IsOpen;
                        allorder.Products = item.Products;
                        allorder.Signature = item.Signature;
                        allorder.TotalPrice = item.TotalPrice;

                        //add allorders to list
                        orderList.Add(allorder);
                    }
                }
            }

            //Sort order list by date Ascending
            orderList.Sort((o1, o2) => DateTime.Compare(o2.DateTime, o1.DateTime));
            context.Logger.LogLine($"Found {page.Count} orders");

            //Response
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = LowercaseJsonSerializer.SerializeObject(orderList),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
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
            var orders = new CustomerOrders();
            try
            {
                orders = await DDBContext.LoadAsync<CustomerOrders>(customerId);
                orders.Orders.Sort((o1, o2) => DateTime.Compare(o2.DateTime, o1.DateTime));
            }
            catch (Exception e)
            {
                //check if customer exist in orders table
                context.Logger.LogLine($"There was an error: {e}");
            }


            //check if orders exist in customer
            if (orders == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "message", "ORDER_IS_EMPTY" } }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
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
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that adds an order by customer.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> AddOrderAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            const string url = "https://maoproduce-stack-customer-signatures.s3-ap-southeast-2.amazonaws.com/";

            //instantiate new order object
            Orders newOrder = new Orders();
            string customerId = null;
            string lastOrderId;

            //check for customerId parameter from url
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.QueryStringParameters[ID_QUERY_STRING_NAME];


            //GET the last order number
            var search = this.DDBContext.ScanAsync<CustomerOrders>(null);
            var page = await search.GetNextSetAsync();
            List<int> list = new List<int>();


            if (!page.Any())
            {
                lastOrderId = "17050";
            }
            else
            {
                foreach (var customer in page)
                {
                    list.Add(int.Parse(customer.LastOrderId));
                }

                lastOrderId = (list.Max() + 1).ToString();
            }

            //get the request body
            var requestOrder = JsonConvert.DeserializeObject<Order_AllOrders>(request?.Body);

            //Convert sent All Orders to Orders Model
            newOrder.Id = lastOrderId;
            newOrder.DateTime = DateTime.Now;
            newOrder.IsOpen = requestOrder.IsOpen;


            //pass signature data to AWSS3BucketSave Function
            string signatureTitle = newOrder.Id + "-" + String.Format("{0}.png", DateTime.Now.ToString("ddMMyyyyhhmmsstt"));
            AWSS3BucketSave bucket = new AWSS3BucketSave();
            await bucket.WritingAnObjectAsync(requestOrder.Signature.Signature, signatureTitle);


            //New instance of signatture with url
            SignatureDetails sig = new SignatureDetails();
            sig.Signature = url + signatureTitle;
            sig.Signee = requestOrder.Signature.Signee;


            //Save new signature object
            newOrder.Signature = sig;
            newOrder.TotalPrice = requestOrder.TotalPrice;
            newOrder.Products = requestOrder.Products;
            

            ////load current customer data in dynamodb order table
            var custNewOrder = await DDBContext.LoadAsync<CustomerOrders>(customerId);
            if (custNewOrder != null)
            {
                custNewOrder.LastOrderId = lastOrderId;
                custNewOrder.Orders.Add(newOrder);

                //Save the order in the right customer.
                var saveOrder = DDBContext.SaveAsync<CustomerOrders>(custNewOrder);
            }
            else
            {
                CustomerOrders newCustOrder = new CustomerOrders();
                newCustOrder.CustomerId = customerId;
                newCustOrder.LastOrderId = lastOrderId;
                newCustOrder.addList(newOrder);

                //Save to Dynamodb
                var saveOrder = DDBContext.SaveAsync<CustomerOrders>(newCustOrder);
            }


            //create success response
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "message", "Order sucessfully created" }, { "orderId", lastOrderId } }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that removes an order post from Customer in Orders table.
        /// </summary>
        /// <param name="request"></param>
        public async Task<APIGatewayProxyResponse> RemoveOrderAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string customerId = null;
            string orderId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.PathParameters[ID_QUERY_STRING_NAME];
            if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("orderId"))
                orderId = request.QueryStringParameters["orderId"];

            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(customerId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }


            context.Logger.LogLine($"Deleting order with Customer: {customerId} AND id: {orderId}");

            var cust = await DDBContext.LoadAsync<CustomerOrders>(customerId);
            if (cust != null)
            {
                foreach (var order in cust.Orders.ToList())
                {
                    if (order.Id == orderId)
                    {
                        cust.removeList(order);
                    }
                }

                //Update Dynamodb
                await DDBContext.SaveAsync<CustomerOrders>(cust);
            }
            else
            {
                context.Logger.LogLine("There is an error please call the server master");
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "message", $"Sucessfully deleted the item: {orderId}" } }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            };
        }



        /// <summary>
        /// A Lambda function that updates an order.
        /// </summary>
        /// <param name="request"></param>
        public async Task<APIGatewayProxyResponse> UpdateOrderAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string customerId = null;
            string orderId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.PathParameters[ID_QUERY_STRING_NAME];
            if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("orderId"))
                orderId = request.QueryStringParameters["orderId"];

            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(customerId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            //get the request body
            var requestOrder = JsonConvert.DeserializeObject<Order_AllOrders>(request?.Body);


            //update the needed data
            var currentOrder = await DDBContext.LoadAsync<CustomerOrders>(customerId);
            if (currentOrder != null)
            {
               foreach(var order in currentOrder.Orders.ToList())
                {
                    if (order.Id == orderId)
                    {
                        order.TotalPrice = requestOrder.TotalPrice;
                        order.IsOpen = requestOrder.IsOpen;
                        order.Products = requestOrder.Products;
                        order.Signature = requestOrder.Signature;
                    }
                }

                //Update Dynamodb
                await DDBContext.SaveAsync<CustomerOrders>(currentOrder);
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "message", $"Sucessfully updated the item: {orderId}" } }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };
            }
            else
            {
                context.Logger.LogLine("There is an error please call the server master");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "message", $"Order not found: {orderId}" } }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };
            }
        }
    }
}
