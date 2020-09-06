using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.IO;

namespace MaoProduce_delivery_app.Functions
{
    public class AWSS3BucketSave
    {
        private const string bucketName = "maoproduce-stack-customer-signatures";
        // For simplicity the example creates two objects from the same file.
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.APSoutheast2;
        private static IAmazonS3 client;
        public async Task WritingAnObjectAsync(string signatureData, string dataTitle)
        {
            client = new AmazonS3Client(bucketRegion);
            var bytearray = Convert.FromBase64String(signatureData);


            //string imageData = Encoding.UTF8.GetString(bytearray);
            try
            {
                using (var stream = new MemoryStream(bytearray, 0, bytearray.Length))
                {
                    //Put the object-set ContentType and add metadata.
                    var putRequest2 = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = dataTitle,
                        InputStream = stream,
                        ContentType = "image/png"
                    };
                    PutObjectResponse response2 = await client.PutObjectAsync(putRequest2);
                }
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
