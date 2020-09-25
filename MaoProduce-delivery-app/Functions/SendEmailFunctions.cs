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
using MaoProduce_delivery_app.Models;
using System.Globalization;

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
        IDynamoDBContext DDBContext2 { get; set; }
        AmazonDynamoDBClient DDBClient = new AmazonDynamoDBClient();
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
            this.DDBContext = new DynamoDBContext(DDBClient, config);
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

            AWSConfigsDynamoDB.Context.TypeMappings[typeof(Customers)] = new Amazon.Util.TypeMapping(typeof(Customers), "MaoProduce-Stack-CustomerTable-OZ2X2B0G09V6");
            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext2 = new DynamoDBContext(new AmazonDynamoDBClient(), config);
        }

        ///Function that sends an email with receipt attached
        public async Task<APIGatewayProxyResponse> SendEmailAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            //Email Data Properties
            string htmlHeader;
            string customerId = null;
            string orderId = null;
            string year = DateTime.Now.ToString("yyyy");
            string totalPrice = null;
            string subTotal;
            string signee = null;
            string signatureUrl = null;
            string quantity;
            string price;
            string description;
            string customer_name;
            List<OrderProduct> orderBody = new List<OrderProduct>();
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-NZ");
            culture.NumberFormat.CurrencyNegativePattern = 1;


            //Get HTTP URL PARAMETERS
            if (request.PathParameters != null && request.PathParameters.ContainsKey("CustomerId"))
                customerId = request.PathParameters["CustomerId"];
            if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("orderId"))
                orderId = request.QueryStringParameters["orderId"];

            //Null-check Get PARAMS ((RULE: customer ID cannot be null; if order id is null create new order else edit current order with orderId param))
            if (!string.IsNullOrEmpty(customerId) && string.IsNullOrEmpty(orderId))
            {
                //Create new instance of orderFunctions -> call add order 
                var create = new OrderFunctions(this.DDBClient, "MaoProduce-Stack-OrderTable-1DCW9ZSV63ZU7");
                await create.AddOrderAsync(request, context);
            }
            else if (!string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(orderId))
            {
                //update existing
                var update = new OrderFunctions(this.DDBClient, "MaoProduce-Stack-OrderTable-1DCW9ZSV63ZU7");
                await update.UpdateOrderAsync(request, context);
            }
            else
            {
                //return 404 error
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "error", "CUSTOMER_NOT_FOUND" } }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };
            }

            
            try
            {
                //First get the customer details
                LoadDatabase();
                var cust = await DDBContext2.LoadAsync<Customers>(customerId);

                //Assign customer details to the right property
                customer_name = cust.Name;

                //Secondly get the order details
                var order = await DDBContext.LoadAsync<CustomerOrders>(customerId);

                if (string.IsNullOrEmpty(orderId))
                {
                    orderId = order.LastOrderId;
                }
                //Assign orderdetails to the right property
                foreach (var item in order.Orders)
                {
                    if (item.Id == orderId)
                    {
                        orderId = item.Id;
                        signee = item.Signature.Signee;
                        signatureUrl = item.Signature.Signature;
                        totalPrice = item.TotalPrice;
                        orderBody = item.Products;
                    }
                }

                //Assign the products in order
                if (string.IsNullOrEmpty(signatureUrl))
                {
                    htmlHeader = $"<center> <table width=\"100%\" style=\"max-width:620px;margin:0 auto;border:1px solid black; background-color:#ffffff\" bgcolor=\"#ffffff\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"> <tbody><tr> <td width=\"100%\" style=\"text-align:left\"> <table width=\"100%\" cellspacing=\"0\" border=\"0\" cellpadding=\"20\" bgcolor=\"#ffffff\" style=\"background-color:#ffffff\"> <tbody><tr> <td width=\"30\"> <center> <img width=\"170px\" height=\"auto\" style=\"width:170px;height:auto\" alt=\"Mao Produce\" src=\"https://shop.maoproduce.co.nz/image/catalog/logo.png\" data-image-whitelisted=\"\"> <h2 style=\"color:#3a9821; font-family: Arial, Helvetica, sans-serif; font-size:34px; font-weight: normal; line-height: 1.3;\">Dispatch Slip</h2> </center> </td> </tr> </tbody></table> </td> </tr> <tr> <td width=\"100%\" style=\"text-align:left\"> <table width=\"100%\" cellspacing=\"20\" border=\"0\" cellpadding=\"20\" bgcolor=\"#3a9821\" style=\"background-color:#ffffff\"> <tbody><tr> <td bgcolor=\"#ffffff\" style=\"background-color:#ffffff\"> <h2 style=\"font-family:Arial, Helvetica, sans-serif; line-height: 1.3; font-weight: normal; font-size:20px\">Hi, {customer_name}</h2> <p style=\"font-family:Arial, Helvetica, sans-serif; line-height: 1.3; font-weight: normal; font-size: 15px; color:#474747\">You will find the order details in this email. <br> If you have any questions according to your order or about, please let us know.<br> Thanks for choosing Mao Produce. <br> </p><table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\"> <tbody><tr> <td valign=\"top\" width=\"50%\"> <table border=\"0\" cellspacing=\"0\" cellpadding=\"3\"> <tbody><tr> <td valign=\"top\">Order #:</td> <td valign=\"top\"><strong>{orderId}</strong></td> </tr> <tr> <td valign=\"top\">Signee:</td> <td valign=\"top\"> <strong>{signee}</strong> </td> <td> </td> </tr> <tr> <td>&nbsp;</td> </tr> </tbody></table> </td> <td valign=\"top\" width=\"50%\"> </td> </tr></tbody></table><hr> <div style=\"font-size:11px\"> <table style=\"width:100%\" cellspacing=\"0\" cellpadding=\"3\"> <thead> <tr> <th valign=\"top\" align=\"left\" style=\"background:#e3e3e3\">Description</th> <th valign=\"top\" align=\"right\" width=\"30\" style=\"background:#e3e3e3\">Qty</th> <th valign=\"top\" align=\"right\" width=\"100\" style=\"background:#e3e3e3\">Price</th> <th colspan=\"2\" valign=\"top\" align=\"right\" width=\"100\" style=\"background:#e3e3e3\">Sub-Total</th> </tr> </thead> <tbody>";
                }
                else
                {
                    htmlHeader = $"<center> <table width=\"100%\" style=\"max-width:620px;margin:0 auto;border:1px solid black; background-color:#ffffff\" bgcolor=\"#ffffff\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"> <tbody><tr> <td width=\"100%\" style=\"text-align:left\"> <table width=\"100%\" cellspacing=\"0\" border=\"0\" cellpadding=\"20\" bgcolor=\"#ffffff\" style=\"background-color:#ffffff\"> <tbody><tr> <td width=\"30\"> <center> <img width=\"170px\" height=\"auto\" style=\"width:170px;height:auto\" alt=\"Mao Produce\" src=\"https://shop.maoproduce.co.nz/image/catalog/logo.png\" data-image-whitelisted=\"\"> <h2 style=\"color:#3a9821; font-family: Arial, Helvetica, sans-serif; font-size:34px; font-weight: normal; line-height: 1.3;\">Dispatch Slip</h2> </center> </td> </tr> </tbody></table> </td> </tr> <tr> <td width=\"100%\" style=\"text-align:left\"> <table width=\"100%\" cellspacing=\"20\" border=\"0\" cellpadding=\"20\" bgcolor=\"#3a9821\" style=\"background-color:#ffffff\"> <tbody><tr> <td bgcolor=\"#ffffff\" style=\"background-color:#ffffff\"> <h2 style=\"font-family:Arial, Helvetica, sans-serif; line-height: 1.3; font-weight: normal; font-size:20px\">Hi, {customer_name}</h2> <p style=\"font-family:Arial, Helvetica, sans-serif; line-height: 1.3; font-weight: normal; font-size: 15px; color:#474747\">You will find the order details in this email. <br> If you have any questions according to your order or about, please let us know.<br> Thanks for choosing Mao Produce. <br> </p><table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\"> <tbody><tr> <td valign=\"top\" width=\"50%\"> <table border=\"0\" cellspacing=\"0\" cellpadding=\"3\"> <tbody><tr> <td valign=\"top\">Order #:</td> <td valign=\"top\"><strong>{orderId}</strong></td> </tr> <tr> <td valign=\"top\">Signee:</td> <td valign=\"top\"> <strong>{signee}</strong> </td> <td> <img width=\"100px\" height=\"auto\" src=\"{signatureUrl}\" valign=\"bottom\" alt=\"signature\" align=\"center\" class=\"text-center\"></td> </tr> <tr> <td>&nbsp;</td> </tr> </tbody></table> </td> <td valign=\"top\" width=\"50%\"> </td> </tr></tbody></table><hr> <div style=\"font-size:11px\"> <table style=\"width:100%\" cellspacing=\"0\" cellpadding=\"3\"> <thead> <tr> <th valign=\"top\" align=\"left\" style=\"background:#e3e3e3\">Description</th> <th valign=\"top\" align=\"right\" width=\"30\" style=\"background:#e3e3e3\">Qty</th> <th valign=\"top\" align=\"right\" width=\"100\" style=\"background:#e3e3e3\">Price</th> <th colspan=\"2\" valign=\"top\" align=\"right\" width=\"100\" style=\"background:#e3e3e3\">Sub-Total</th> </tr> </thead> <tbody>";
                }
                string htmlHeaderNeg = "";
                foreach (var products in orderBody)
                {
                    description = products.Title;
                    quantity = String.Format("{0:0.#}", double.Parse(products.Quantity));
                    price = String.Format(culture, "{0:C}", double.Parse(products.Price));
                    subTotal = String.Format(culture, "{0:C}", (double.Parse(products.Quantity) * double.Parse(products.Price)));

                    //check if its a return
                    if (double.Parse(products.Quantity) < 0 || double.Parse(products.Price) < 0)
                    {
                        // Display RED VERSION
                        htmlHeaderNeg += $"<tr style=\"color:red;\"> <td valign=\"top\" align=\"left\">{description}</p></td> <td valign=\"top\" align=\"right\" width=\"30\">{quantity}</td> <td valign=\"top\" align=\"right\" width=\"100\">{price}</td> <td colspan=\"2\" valign=\"top\" align=\"right\" width=\"100\">{subTotal}</td></tr> ";
                    }
                    else
                    {
                        // The HTML body of the email.
                        htmlHeader += $"<tr> <td valign=\"top\" align=\"left\">{description}</td> <td valign=\"top\" align=\"right\" width=\"30\">{quantity}</td> <td valign=\"top\" align=\"right\" width=\"100\">{price}</td> <td colspan=\"2\" valign=\"top\" align=\"right\" width=\"100\">{subTotal}</td> </tr> ";
                    }
                }
                htmlHeader += htmlHeaderNeg;
                if (double.Parse(totalPrice) < 0)
                    htmlHeader += $"<tr> <td align=\"left\" colspan=\"5\" style=\"border-bottom:solid 1px #e3e3e3\"></td> </tr> </tbody> <tfoot> <tr> <td align=\"right\" colspan=\"4\" style=\"background:#e3e3e3\"><strong>Total:</strong></td> <td align=\"right\" style=\"background:#e3e3e3\"><strong style=\"color:red;\">{String.Format(culture, "{0:C}", double.Parse(totalPrice))}</strong></td> </tr> </tfoot> </table> </div><p><em style=\"font-family: Arial, Helvetica, sans-serif; font-size: 12px; color:#777777\"> <br> <center> \u00A9 Copyright © {year} <a style=\"text-decoration:none; color:#3a9821;\" href=\"https://www.maoproduce.co.nz\">Mao Produce</a> All Rights Reserved. <br> Powered by <a style=\"text-decoration:none; color:darkred;\" href=\"https://canit.co.nz\">CanIT Ltd.</a></p> </center></em></p> </td> </tr> </tbody></table> </td> </tr> </tbody></table> </center>";
                else
                    htmlHeader += $"<tr> <td align=\"left\" colspan=\"5\" style=\"border-bottom:solid 1px #e3e3e3\"></td> </tr> </tbody> <tfoot> <tr> <td align=\"right\" colspan=\"4\" style=\"background:#e3e3e3\"><strong>Total:</strong></td> <td align=\"right\" style=\"background:#e3e3e3\"><strong>{String.Format(culture, "{0:C}", double.Parse(totalPrice))}</strong></td> </tr> </tfoot> </table> </div><p><em style=\"font-family: Arial, Helvetica, sans-serif; font-size: 12px; color:#777777\"> <br> <center> \u00A9 Copyright © {year} <a style=\"text-decoration:none; color:#3a9821;\" href=\"https://www.maoproduce.co.nz\">Mao Produce</a> All Rights Reserved. <br> Powered by <a style=\"text-decoration:none; color:darkred;\" href=\"https://canit.co.nz\">CanIT Ltd.</a></p> </center></em></p> </td> </tr> </tbody></table> </td> </tr> </tbody></table> </center>";

                //First setup email/template
                string senderAddress = "Mao Produce <george@canit.co.nz>";
                string receiverAddress = cust.Email;
                string configSet = "ConfigSet";

                // The subject line for the email.
                string subject = $"Mao Produce Delivery Confirmation.";

                // The email body for recipients with non-HTML email clients.
                string textBody = @$"Hi, {customer_name}, 
                            Thank you for your order with Mao Produce. 
                            We have recoreded your order: {orderId}.
                            Please view this email in a updated browser to see more details.
                            Thank You!
                            Mao Produce";


                // Set-up to SES client
                using var client = new AmazonSimpleEmailServiceV2Client(RegionEndpoint.APSoutheast2);
                var sendRequest = new SendEmailRequest()
                {
                    ConfigurationSetName = configSet,
                    FromEmailAddress = senderAddress,
                    ReplyToAddresses = new List<string> { "admin@maoproduce.co.nz" },
                    Destination = new Destination
                    {
                        ToAddresses =
                        new List<string> { receiverAddress }
                    },
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
                                    Data = htmlHeader
                                },
                                Text = new Content
                                {
                                    Data = textBody
                                }
                            }
                        }

                    },
                };


                //Run email client
                var response = await client.SendEmailAsync(sendRequest);
                if (response.HttpStatusCode == HttpStatusCode.OK){
                    context.Logger.LogLine("The email was sent successfully.");
                }

                
                var res = new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string> { {"message", "EMAIL_SENT" }, { "orderId", orderId } }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
                };
                orderId = null;
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
