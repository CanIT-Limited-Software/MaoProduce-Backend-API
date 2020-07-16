using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Newtonsoft.Json;

using Xunit;

namespace MaoProduce_delivery_app.Tests
{
    public class FunctionTest : IDisposable
    { 
        string TableName { get; }
        IAmazonDynamoDB DDBClient { get; }
        
        public FunctionTest()
        {
            this.TableName = "MaoProduce-delivery-app-Customers-" + DateTime.Now.Ticks;
            this.DDBClient = new AmazonDynamoDBClient(RegionEndpoint.USWest2);

            SetupTableAsync().Wait();
        }

        [Fact]
        public async Task CustomersTestAsync()
        {
            TestLambdaContext context;
            APIGatewayProxyRequest request;
            APIGatewayProxyResponse response;

            CustomerFunctions functions = new CustomerFunctions(this.DDBClient, this.TableName);


            // Add a new blog post
            Customers cust = new Customers();
            cust.Name = "The awesome post";
            cust.Email = "Content for the awesome blog";

            request = new APIGatewayProxyRequest
            {
                Body = JsonConvert.SerializeObject(cust)
            };
            context = new TestLambdaContext();
            response = await functions.AddCustomerAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            var blogId = response.Body;

            // Confirm we can get the blog post back out
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { CustomerFunctions.ID_QUERY_STRING_NAME, blogId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetCustomerAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            Customers read = JsonConvert.DeserializeObject<Customers>(response.Body);


            // List the blog posts
            request = new APIGatewayProxyRequest
            {
            };
            context = new TestLambdaContext();
            response = await functions.GetCustomersAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            Customers[] blogPosts = JsonConvert.DeserializeObject<Customers[]>(response.Body);
			Assert.Single(blogPosts);



            // Delete the blog post
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { CustomerFunctions.ID_QUERY_STRING_NAME, blogId } }
            };
            context = new TestLambdaContext();
            response = await functions.RemoveCustomerAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            // Make sure the post was deleted.
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { CustomerFunctions.ID_QUERY_STRING_NAME, blogId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetCustomerAsync(request, context);
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }



        /// <summary>
        /// Create the DynamoDB table for testing. This table is deleted as part of the object dispose method.
        /// </summary>
        /// <returns></returns>
        private async Task SetupTableAsync()
        {
            
            CreateTableRequest request = new CreateTableRequest
            {
                TableName = this.TableName,
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 2,
                    WriteCapacityUnits = 2
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        KeyType = KeyType.HASH,
                        AttributeName = CustomerFunctions.ID_QUERY_STRING_NAME
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = CustomerFunctions.ID_QUERY_STRING_NAME,
                        AttributeType = ScalarAttributeType.S
                    }
                }
            };

            await this.DDBClient.CreateTableAsync(request);

            var describeRequest = new DescribeTableRequest { TableName = this.TableName };
            DescribeTableResponse response = null;
            do
            {
                Thread.Sleep(1000);
                response = await this.DDBClient.DescribeTableAsync(describeRequest);
            } while (response.Table.TableStatus != TableStatus.ACTIVE);
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.DDBClient.DeleteTableAsync(this.TableName).Wait();
                    this.DDBClient.Dispose();
                }

                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(true);
        }
        #endregion


    }
}
