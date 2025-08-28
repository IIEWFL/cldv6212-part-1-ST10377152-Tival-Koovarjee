using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace ABC_Retail.Services.Storage
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public BlobStorageService(string storageConnectionString, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            _blobContainerClient.CreateIfNotExists();
        }

        //upload photo create.cshtml
        public async Task<string> UploadPhotoAsync(string blobName, Stream stream) 
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(stream, true);
            return GetBlobUriWithSas(blobClient);
        }
        //get blob uri with SAS token
        private string GetBlobUriWithSas(BlobClient blobClient) 
        {
            if (blobClient.CanGenerateSasUri)
            {
                var sasbuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
                };
                sasbuilder.SetPermissions(BlobSasPermissions.Read);
                var sasUri = blobClient.GenerateSasUri(sasbuilder);
                return sasUri.ToString();
            }
            else 
            {
                throw new InvalidOperationException("Blob client does not support generating SAS URIs");
            }

        }
        //delete photo delete.cshtml
        public async Task DeletePhotoAsync(string blobName) 
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
