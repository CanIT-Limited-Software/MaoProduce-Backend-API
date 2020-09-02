using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;

namespace MaoProduce_delivery_app.Functions
{
    class AWSS3BucketSave
    {
        private const string bucketName = "maoproduce-stack-customer-signatues";
        // For simplicity the example creates two objects from the same file.
        // You specify key names for these objects.
        private const string keyName1 = "Object1";
        private const string keyName2 = "ObjectKeyName2";
        private const string filePath = @"*** file path ***";
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.APSoutheast2;

        private static IAmazonS3 client;

        public static void Main()
        {
            string test = "<svg width=\"400\" height=\"180\">\r\n  <rect x=\"50\" y=\"20\" width=\"150\" height=\"150\" style=\"fill:blue;stroke:pink;stroke-width:5;fill-opacity:0.1;stroke-opacity:0.9\" />\r\n  Sorry, your browser does not support inline SVG.  \r\n</svg>";
            client = new AmazonS3Client(bucketRegion);
            WritingAnObjectAsync(test).Wait();
        }

        static async Task WritingAnObjectAsync(string svgData)
        {
            try
            {
                // 1. Put object-specify only key name for the new object.
                var putRequest1 = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName1,
                    ContentBody = "sample text"
                };

                PutObjectResponse response1 = await client.PutObjectAsync(putRequest1);

                // 2. Put the object-set ContentType and add metadata.
                var putRequest2 = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName2,
                    ContentBody = svgData,
                    ContentType = "image/svg+xml"
                };

                PutObjectResponse response2 = await client.PutObjectAsync(putRequest2);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(
                        "Error encountered ***. Message:'{0}' when writing an object"
                        , e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unknown encountered on server. Message:'{0}' when writing an object"
                    , e.Message);
            }
        }
    }
}
