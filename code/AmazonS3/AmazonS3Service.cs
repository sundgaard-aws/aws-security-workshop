using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.S3
{
    public class AmazonS3Service : IObjectStoreService
    {
        private AmazonS3Client s3Client;
        private string TempPath { get { return Path.Join(Path.GetTempPath(),"s3"); } }


        public AmazonS3Service(RegionEndpoint region) {
            s3Client=new AmazonS3Client(region);
        }

        public async Task<string> GetObjectAsyncAsString(string objectStoreName, string objectName)
        {
            var getObjectRequest=new GetObjectRequest{
                BucketName=objectStoreName,
                Key=objectName
            };
            var objResponse=await s3Client.GetObjectAsync(getObjectRequest);
            return new StreamReader(objResponse.ResponseStream).ReadToEnd();
        }

        public async Task<FileInfo> GetObjectAsync(string objectStoreName, string objectName)
        {
            Console.WriteLine($"Looking for object with key {objectName} in bucket {objectStoreName}");
            var getObjectRequest=new GetObjectRequest{
                BucketName=objectStoreName,
                Key=objectName
            };
            var objResponse=await s3Client.GetObjectAsync(getObjectRequest);
            var tmpFilePath=Path.Join(TempPath, objectName);
            var tmpFile=new FileInfo(tmpFilePath);
            await objResponse.WriteResponseStreamToFileAsync(tmpFilePath, false, default(System.Threading.CancellationToken));
            return tmpFile;
        }        

        public async Task UploadObjectAsync(string objectStoreName, FileInfo fileToUpload) {
            var putObjectRequest=new PutObjectRequest{
                BucketName=objectStoreName,
                Key=fileToUpload.Name,
                FilePath=fileToUpload.FullName
            };
            await s3Client.PutObjectAsync(putObjectRequest);
        }
    }
}