using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal.Util;
using MaoProduce_delivery_app.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

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


        ///Function that Generates the PDF
        protected void GeneratePDF()
        {
            //first get customer and order id's

            //get customer details

            //get order details

            //setup pdf/template

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();
                document.Add(new Paragraph("Hello World"));
                document.Close();
                writer.Close();

                //SET UP EMAIL FIRST WITH AWS LAMBDA .NET(c#) -> AWS SES sdk
                //Instead use email to test pdf


                //Response.ContentType = "pdf/application";
                //Response.AddHeader("content-disposition",
                //"attachment;filename=First PDF document.pdf");
                //Response.OutputStream.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
            }


            //generate the pdf file

            //return???
        }


        ///Function that sends an email with receipt attached
        public void SendEmail()
        {
            //First setup email/template

            //attach pdf file
            GeneratePDF();

            //send email
        }
    }
}