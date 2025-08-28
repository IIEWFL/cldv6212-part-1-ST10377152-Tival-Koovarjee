using ABC_Retail.Models;
using ABC_Retail.Services.Storage;

namespace ABC_Retail.Services
{
    public class CustomerService
    {
        private readonly TableStorageService<Customer> _tableStorageService;
        private readonly BlobStorageService _blobStorageService;

        public CustomerService(string storageConnectionString, string tableName, BlobStorageService blobStorageService)
        {
            _tableStorageService = new TableStorageService<Customer>(storageConnectionString, tableName);
            _blobStorageService = blobStorageService;
        }

        public Task<List<Customer>> GetCustomersAsync()
        {
            return _tableStorageService.GetAllAsync();
        }

        public Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            return _tableStorageService.GetAsync(partitionKey, rowKey);
        }

        public async Task AddCustomerAsync(Customer customer, Stream? imageStream = null, string? blobName = null)
        {
            if (imageStream != null && blobName != null)
            {
                customer.PhotoUrl = await _blobStorageService.UploadPhotoAsync(blobName, imageStream);
            }
            await _tableStorageService.AddAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer, Stream? newImageStream = null, string? newBlobName = null)
        {
            if (newImageStream != null && !string.IsNullOrEmpty(newBlobName))
            {
                if (!string.IsNullOrEmpty(customer.PhotoUrl))
                {
                    await _blobStorageService.DeletePhotoAsync(customer.PhotoUrl);
                }
                customer.PhotoUrl = await _blobStorageService.UploadPhotoAsync(newBlobName, newImageStream);
            }
            await _tableStorageService.UpdateAsync(customer);
        }

        public Task DeleteCustomerAsync(string partitionKey, string rowKey) 
        {
            return _tableStorageService.DeleteAsync(partitionKey, rowKey);
        }
    }
}
