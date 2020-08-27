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
        //OTENTOTENTEOTN
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
            string htmlBody = "<center> <table width=\"100%\" style=\"max-width:620px;margin:0 auto;border:1px solid black; background-color:#ffffff\" bgcolor=\"#ffffff\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"> <tbody><tr> <td width=\"100%\" style=\"text-align:left\"> <table width=\"100%\" cellspacing=\"0\" border=\"0\" cellpadding=\"20\" bgcolor=\"#ffffff\" style=\"background-color:#ffffff\"> <tbody><tr> <td width=\"30\"> <center> <img width=\"170px\" height=\"auto\" style=\"width:170px;height:auto\" alt=\"Mao Produce\" src=\"https://shop.maoproduce.co.nz/image/catalog/logo.png\" data-image-whitelisted=\"\"> <h2 style=\"color:#3a9821; font-family: Arial, Helvetica, sans-serif; font-size:34px; font-weight: normal; line-height: 1.3;\">Dispatch Slip</h2> </center> </td> </tr> </tbody></table> </td> </tr> <tr> <td width=\"100%\" style=\"text-align:left\"> <table width=\"100%\" cellspacing=\"20\" border=\"0\" cellpadding=\"20\" bgcolor=\"#3a9821\" style=\"background-color:#ffffff\"> <tbody><tr> <td bgcolor=\"#ffffff\" style=\"background-color:#ffffff\"> <h2 style=\"font-family:Arial, Helvetica, sans-serif; line-height: 1.3; font-weight: normal; font-size:20px\">Hi, {{customer_name}}</h2> <p style=\"font-family:Arial, Helvetica, sans-serif; line-height: 1.3; font-weight: normal; font-size: 15px; color:#474747\">You will find the order details in this email. <br> If you have any questions according to your order or about, please let us know.<br> Thanks for choosing Mao Produce. <br> </p><table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\"> <tbody><tr> <td valign=\"top\" width=\"50%\"> <table border=\"0\" cellspacing=\"0\" cellpadding=\"3\"> <tbody><tr> <td valign=\"top\">Order Number:</td> <td valign=\"top\"><strong>{{orderId}}</strong></td> </tr> <tr> <td valign=\"top\">Signee:</td> <td valign=\"top\"> <strong>{{signee}}</strong> </td> <td> <img width=\"100px\" height=\"auto\" src=\"{{signatureUrl}}\" valign=\"bottom\" alt=\"signature\" align=\"center\" class=\"text-center\"> </td> </tr> <tr> <td>&nbsp;</td> </tr> </tbody></table> </td> <td valign=\"top\" width=\"50%\"> </td> </tr></tbody></table><hr> <div style=\"font-size:11px\"> <table style=\"width:100%\" cellspacing=\"0\" cellpadding=\"3\"> <thead> <tr> <th valign=\"top\" align=\"left\" style=\"background:#e3e3e3\">Description</th> <th valign=\"top\" align=\"right\" width=\"30\" style=\"background:#e3e3e3\">Qty</th> <th valign=\"top\" align=\"right\" width=\"100\" style=\"background:#e3e3e3\">Price</th> <th colspan=\"2\" valign=\"top\" align=\"right\" width=\"100\" style=\"background:#e3e3e3\">Sub-Total</th> </tr> </thead> <tbody> {{#each item}} <tr> <td valign=\"top\" align=\"left\">{{description}}</td> <td valign=\"top\" align=\"right\" width=\"30\">{{quantity}}</td> <td valign=\"top\" align=\"right\" width=\"100\">${{price}}</td> <td colspan=\"2\" valign=\"top\" align=\"right\" width=\"100\">${{subTotal}}</td> </tr> {{/each}} <tr> <td align=\"left\" colspan=\"5\" style=\"border-bottom:solid 1px #e3e3e3\"></td> </tr> </tbody> <tfoot> <tr> <td align=\"right\" colspan=\"4\" style=\"background:#e3e3e3\">Total:</td> <td align=\"right\" style=\"background:#e3e3e3\">${{totalPrice}}</td> </tr> </tfoot> </table> </div><p><em style=\"font-family: Arial, Helvetica, sans-serif; font-size: 12px; color:#777777\"> <br> <center> \u00A9 Copyright {{year}} Powered by <a style=\"color:#3a9821;\" href=\"https://canit.co.nz\">CanIT Limited.</a> All Rights Reserved.</p> </center></em></p> </td> </tr> </tbody></table> </td> </tr> </tbody></table> </center>";

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
                    ////Trying to send the templated email from ses
                    //Template = new Template
                    //{
                    //    TemplateData = "{}",
                    //    TemplateName = "MaoProduce-Email-Template-4"
                    //},

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

                        // If you are not using a configuration set, comment
                        // or remove the following line 
                        //ConfigurationSetName = configSet
                }
            };
            try
            {
                context.Logger.LogLine("Sending email using Amazon SES...");
                var response = await client.SendEmailAsync(sendRequest);
                if (response.HttpStatusCode == HttpStatusCode.OK){
                    context.Logger.LogLine(JsonConvert.SerializeObject(response));
                    context.Logger.LogLine("The email was sent successfully.");
                }
                

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