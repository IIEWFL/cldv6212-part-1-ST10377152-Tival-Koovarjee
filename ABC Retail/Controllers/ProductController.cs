using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ABC_Retail.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly BlobStorageService _blobStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareStorageService fileShareStorageService;

        public ProductController(Services.ProductService productService, BlobStorageService blobStorageService, QueueStorageService queueStorageService, FileShareStorageService fileShareStorageService)
        {
            _productService = productService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
            this.fileShareStorageService = fileShareStorageService;
        }

        // GET: Product/Index
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetProductsAsync();
            return View(products);
        }

        // GET: /Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Products products, IFormFile? image)
        {
            if (ModelState.IsValid)
            {
                var productValue = new Products
                {
                    PartitionKey = "product",
                    RowKey = Guid.NewGuid().ToString(),
                    ProductId = Guid.NewGuid().ToString(),
                    ProductName = products.ProductName,
                    Description = products.Description,
                    Price = products.Price,
                    Stock = products.Stock
                };
                //Upload photo to blob and return the SAS URL
                if (image != null)
                {
                    using var stream = image.OpenReadStream();
                    products.PhotoUrl = await _blobStorageService.UploadPhotoAsync(Guid.NewGuid().ToString(), stream);
                }
                await _productService.AddProductAsync(products);

                var message = new 
                {
                    Action = "New Product Added",
                    Timestamp = DateTime.UtcNow,
                    Details = new 
                    {
                     products.PartitionKey,
                     products.RowKey,
                     products.ProductName,
                     products.Description,
                     products.Price,
                     products.Stock

                    }
                };
                await _queueStorageService.SendMessagesAsync(message);
                return RedirectToAction(nameof(Index));

            }
            return View(products);
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var product = await _productService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        //Get Product/Edit
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var products = await _productService.GetProductAsync(partitionKey, rowKey);
            if (products == null)
            {
                return NotFound();
            }
            return View(products);
        }

        //Post Product/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Products products, IFormFile? newImage)
        {
            if (ModelState.IsValid)
            {
                var existingProducts = await _productService.GetProductAsync(products.PartitionKey!, products.RowKey!);
                if (existingProducts == null)
                {
                    return NotFound();
                }
                existingProducts.ProductName = products.ProductName;
                existingProducts.Description = products.Description;
                existingProducts.Price = products.Price;
                existingProducts.Stock = products.Stock;
                // If a new image is uploaded, replace the old one
                if (newImage != null)
                {
                    using var stream = newImage.OpenReadStream();
                    await _productService.UpdateProductAsync(existingProducts, stream, Guid.NewGuid().ToString());
                }
                else
                {
                    await _productService.UpdateProductAsync(existingProducts);
                }

                var message = new 
                {
                    Action = "Product Updated",
                    Timestamp = DateTime.UtcNow,
                    Details = new 
                    {
                     products.PartitionKey,
                     products.RowKey,
                     products.ProductName,
                     products.Description,
                     products.Price,
                     products.Stock
                    }
                };
                await _queueStorageService.SendMessagesAsync(message);
                return RedirectToAction(nameof(Index));
            }
            return View(products);
        }

        //Get Product/Delete
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var products = await _productService.GetProductAsync(partitionKey, rowKey);
            if (products == null)
            {
                return NotFound();
            }
            return View(products);
        }

        //Post Product/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var products = await _productService.GetProductAsync(partitionKey, rowKey);
            if (products == null)
            {
                return NotFound();
            }
            await _productService.DeleteProductAsync(partitionKey, rowKey);

            var message = new 
            {
                Action = "Product Deleted",
                Timestamp = DateTime.UtcNow,
                Details = new 
                {
                 products.PartitionKey,
                 products.RowKey,
                 products.ProductName,
                 products.Description,
                 products.Price,
                 products.Stock
                }
            };
            await _queueStorageService.SendMessagesAsync(message);
            return RedirectToAction(nameof(Index));
        }

        //get customerLogs/log
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            var logMessages = await _queueStorageService.GetMessagesAsync();
            return View(logMessages);
        }

        //post exportlog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportLog()
        {
            var logMessages = await _queueStorageService.GetMessagesAsync();
            var filename = $"Log_{DateTime.UtcNow:yyyyMMHHmmss}.csv";

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                //write header
                await writer.WriteLineAsync("MessageId, InsertionTime, MessageText");
                //write each log
                foreach (var log in logMessages)
                {
                    //Escape any double quotes in the message text
                    var messageText = log.MessageText?.Replace("\"", "\"\"");
                    //Ensure fieldsare enclosed in double quotes
                    await writer.WriteLineAsync($"\"{log.MessageId}\", \"{log.InsertionTime.ToString("yyyy/MM/dd HH:mm:ss")}\", \"{messageText}\"");
                }
                await writer.FlushAsync();
                //reset the stream postion to the beginning before uploading
                stream.Position = 0;
                await fileShareStorageService.UploadFileAsync(filename, stream);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
