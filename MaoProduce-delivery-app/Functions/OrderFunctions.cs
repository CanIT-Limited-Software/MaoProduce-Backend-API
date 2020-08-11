﻿using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal.Util;
using MaoProduce_delivery_app.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
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


        ///<SUMMMARY>
        /// A Function that will load customer database
        ///</---->
        public void LoadDatabase()
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
        /// <returns>The list of blogs</returns>
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

                        //assign all fields appropriately
                        allorder.CustomerId = customer.CustomerId;
                        allorder.CustomerName = cust.Name;
                        allorder.DateTime = item.DateTime;
                        allorder.Id = item.Id;
                        allorder.IsOpen = item.IsOpen;
                        allorder.Products = item.Products;
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
            var orders = new CustomerOrders();
            try
            {
                orders = await DDBContext.LoadAsync<CustomerOrders>(customerId);
                orders.Orders.Sort((o1, o2) => DateTime.Compare(o2.DateTime, o1.DateTime));
            }catch(Exception e)
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
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string>{ { "message", "ORDER_IS_EMPTY" } }),
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
            //instantiate new customer order object
            CustomerOrders newCustomerOrder = new CustomerOrders();
            Orders newOrder = new Orders();
            string customerId = null;
            string loid;

            //check for customerId parameter from url
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                customerId = request.QueryStringParameters[ID_QUERY_STRING_NAME];


            ////GET the last order number
            var search = this.DDBContext.ScanAsync<CustomerOrders>(null);
            var page = await search.GetNextSetAsync();
            List<int> list = new List<int>();
            
            context.Logger.LogLine("Passed through line 272");
            foreach (var customer in page)
            {
                list.Add(int.Parse(customer.LastOrderId));
            }
            context.Logger.LogLine("Passed through line 277");
            loid = (list.Max() + 1).ToString();
            context.Logger.LogLine($"The highest number in database is: {loid}");


            //get the request body
            var requestOrder = JsonConvert.DeserializeObject<Order_AllOrders>(request?.Body);

            //Convert sent All Orders to Orders Model
            newOrder.Id = loid;
            newOrder.DateTime = DateTime.Now;
            newOrder.IsOpen = requestOrder.IsOpen;
            newOrder.TotalPrice = requestOrder.TotalPrice;
            newOrder.Products = requestOrder.Products;


            //Assign values to new CustomerOrders object
            newCustomerOrder.CustomerId = customerId;
            newCustomerOrder.LastOrderId = loid;
            newCustomerOrder.addList(newOrder);
            context.Logger.LogLine(JsonConvert.SerializeObject(newCustomerOrder));
            //context.Logger.LogLine(JsonConvert.SerializeObject(newOrder));

            //Save the order in the right customer.
            var saveOrder = DDBContext.SaveAsync<CustomerOrders>(newCustomerOrder);


            //create success response
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(new Dictionary<string, string> { {"message", "Order sucessfully created" }, {"orderId", loid} }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
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
