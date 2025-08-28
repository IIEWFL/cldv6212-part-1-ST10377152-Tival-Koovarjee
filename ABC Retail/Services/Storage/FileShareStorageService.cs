using Azure;
using Azure.Storage.Files.Shares;

namespace ABC_Retail.Services.Storage
{
    public class FileShareStorageService
    {
        private readonly ShareClient _shareClient;

        public FileShareStorageService(string storageConnectionString, string shareName)
        {
            var shareServiceClient = new ShareServiceClient(storageConnectionString);
            _shareClient = shareServiceClient.GetShareClient(shareName);
            _shareClient.CreateIfNotExists();
        }

        //upload log to file share
        public async Task UploadFileAsync(string fileName, Stream fileStream) 
        {
            var directoryClient = _shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(fileStream.Length);

            //handling the CSV
            long position = 0;
            int bufferSize = 4 * 1024 * 1024;
            byte [] buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize)) > 0) 
            {
                using var memoryStream = new MemoryStream(buffer, 0, bytesRead);
                await fileClient.UploadRangeAsync(
                Azure.Storage.Files.Shares.Models.ShareFileRangeWriteType.Update,
                new HttpRange(position, bytesRead),memoryStream
                );
                position += bytesRead;
            }
        }
    }
}
