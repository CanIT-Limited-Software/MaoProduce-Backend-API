using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

namespace MaoProduce_delivery_app.Functions
{
    class SendEmailFunctions
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
        public SendEmailFunctions()
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
        public SendEmailFunctions(IAmazonDynamoDB ddbClient, string tableName)
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

        ///Function that sends an email with receipt attached
        public async Task<APIGatewayProxyResponse> SendEmailAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            //First setup email/template
            // Replace sender@example.com with your "From" address.
            // This address must be verified with Amazon SES.
            string senderAddress = "george@canit.co.nz";

            // Replace recipient@example.com with a "To" address. If your account
            // is still in the sandbox, this address must be verified.
            string receiverAddress = "sales@canit.co.nz";

            // The configuration set to use for this email. If you do not want to use a
            // configuration set, comment out the following property and the
            // ConfigurationSetName = configSet argument below. 
            //string configSet = "ConfigSet";

            // The subject line for the email.
            string subject = "Delivery Confirmation for Order: 69696969";

            // The email body for recipients with non-HTML email clients.
            string textBody = @"Hi Vincent, 
                            Thank you for your order with Mao Produce. 
                            Please find the attached order #69696969, for your records.
                            Thank You!
                            Mao Produce";

            // The HTML body of the email.
            string htmlBody = @"<head>
                                  <meta charset='UTF-8' />
                                  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
                                  <link rel='stylesheet' href='https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0/css/bootstrap.min.css'
                                    integrity='sha384-Gn5384xqQ1aoWXA+058RXPxPg6fy4IWvTNh0E263XmFcJlSAwiGgFAW/dAiS6JXm' crossorigin='anonymous' />
                                </head>
                                <style>
                                  .card {
                                    height: 300px;
                                    background-color: green;
                                    color: white;
                                    margin: auto;
                                    width: 50%;
                                    padding: 10px
                                  }
                                </style>

                                <body style='background-color:grey';>
                                     < div class='container'>
                                    <div class='row text-center my-auto'>
                                      <div class='col-12'>
                                        <div class='card'>
                                          <h1 style='color: red;'>Welcome to Mao Produce</h1>
                                        </div>
                                      </div>
                                    </div>
                                  </div>
                                  <script src='https://code.jquery.com/jquery-3.2.1.slim.min.js'
                                    integrity='sha384-KJ3o2DKtIkvYIK3UENzmM7KCkRr/rE9/Qpg6aAZGJwFDMVNA/GpGFF93hXpG5KkN'
                                    crossorigin='anonymous'></script>
                                  <script src='https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.12.9/umd/popper.min.js'
                                    integrity='sha384-ApNbgh9B+Y1QKtv3Rn7W3mgPxhU9K/ScQsAP7hUibX39j7fakFPskvXusvfa0b4Q'
                                    crossorigin='anonymous'></script>
                                  <script src='https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0/js/bootstrap.min.js'
                                    integrity='sha384-JZR6Spejh4U02d8jOt6vLEHfe/JQGiRRSQQxSfFWpi1MquVdAyjUar5+76PVCmYl'
                                    crossorigin='anonymous'></script>
                                </body>";

            // Replace USWest2 with the AWS Region you're using for Amazon SES.
            // Acceptable values are EUWest1, USEast1, and USWest2.
            using var client = new AmazonSimpleEmailServiceV2Client(RegionEndpoint.APSoutheast2);
            var sendRequest = new SendEmailRequest()
            {
                FromEmailAddress = senderAddress,
                Destination = new Destination
                {
                    ToAddresses =
                    new List<string> { receiverAddress },
                    BccAddresses =
                    new List<string> { "draggem0@gmail.com" }

                },
                ReplyToAddresses =
                new List<string> { "admin@maoproduce.co.nz" },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content
                        {
                            Data = subject
                        },
                        Body = new Body
                        {
                            Html = new Content
                            {
                                Data = htmlBody
                            },
                            Text = new Content
                            {
                                Data = textBody
                            }
                        }
                    }
                },
                // If you are not using a configuration set, comment
                // or remove the following line 
                //ConfigurationSetName = configSet
            };
            try
            {
                context.Logger.LogLine("Sending email using Amazon SES...");
                var response = await client.SendEmailAsync(sendRequest);
                context.Logger.LogLine("The email was sent successfully.");

                var res = new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string> { {"message", "EMAIL_SENT" } }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };

                return res;
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("The email was not sent.");
                context.Logger.LogLine("Error message: " + ex.Message);
                var res = new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "error", "EMAIL_FAILED" } }) ,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };

                return res;
            }
        }
     }
    
}