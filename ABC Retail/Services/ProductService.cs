using ABC_Retail.Models;
using ABC_Retail.Services.Storage;

namespace ABC_Retail.Services
{
    public class ProductService
    {
        private readonly TableStorageService<Products> _tableStorageService;
        private readonly BlobStorageService _blobStorageService;

        public ProductService(string storageConnectionString, string tableName, BlobStorageService blobStorageService)
        {
            _tableStorageService = new TableStorageService<Products>(storageConnectionString, tableName);
            _blobStorageService = blobStorageService;
        }

        public Task<List<Products>> GetProductsAsync()
        {
            return _tableStorageService.GetAllAsync();
        }

        public Task<Products?> GetProductAsync(string partitionKey, string rowKey)
        {
            return _tableStorageService.GetAsync(partitionKey, rowKey);
        }

        public async Task AddProductAsync(Products products, Stream? imageStream = null, string? blobName = null)
        {
            if (imageStream != null && blobName != null)
            {
                products.PhotoUrl = await _blobStorageService.UploadPhotoAsync(blobName, imageStream);
            }
            await _tableStorageService.AddAsync(products);
        }

        public async Task UpdateProductAsync(Products products, Stream? newImageStream = null, string? newBlobName = null)
        {
            if (newImageStream != null && !string.IsNullOrEmpty(newBlobName))
            {
                if (!string.IsNullOrEmpty(products.PhotoUrl))
                {
                    await _blobStorageService.DeletePhotoAsync(products.PhotoUrl);
                }
                products.PhotoUrl = await _blobStorageService.UploadPhotoAsync(newBlobName, newImageStream);
            }
            await _tableStorageService.UpdateAsync(products);
        }

        public Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            return _tableStorageService.DeleteAsync(partitionKey, rowKey);
        }
    }
}
