using ABC_Retail.Services;
using ABC_Retail.Services.Storage;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ABC_Retail.Models;
using System.Text;

namespace ABC_Retail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerService _customerService;
        private readonly BlobStorageService _blobStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareStorageService fileShareStorageService;

        public CustomerController(Services.CustomerService customerService, BlobStorageService blobStorageService, QueueStorageService queueStorageService, FileShareStorageService fileShareStorageService)
        {
            _customerService = customerService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
            this.fileShareStorageService = fileShareStorageService;
        }

        // GET: Customer/Index
        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetCustomersAsync();
            return View(customers);
        }

        // GET: /Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer, IFormFile? image) 
        {
            if (ModelState.IsValid) 
            {
                var customerValue = new Customer
                {
                    PartitionKey = "customer",
                    RowKey = Guid.NewGuid().ToString(),
                    CustomerId = Guid.NewGuid().ToString(),
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber
                };
                //Upload photo to blob and return the SAS URL
                if (image != null) 
                {
                    using var stream = image.OpenReadStream();
                    customer.PhotoUrl = await _blobStorageService.UploadPhotoAsync(Guid.NewGuid().ToString(), stream);
                }
                //add cutomer to table storage
                await _customerService.AddCustomerAsync(customerValue);
                

                //send message to queue

                var message = new
                {
                    Action = "New Product Added",
                    TimeStamp = DateTime.UtcNow,
                    Details = new
                    {
                        customer.PartitionKey,
                        customer.RowKey,
                        customer.FirstName,
                        customer.LastName,
                        customer.Email,
                        customer.PhoneNumber
                    }
                };
                await _queueStorageService.SendMessagesAsync(message);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        public  async Task<IActionResult > Details(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        //Get Customer/Edit
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        //Post Customer/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer, IFormFile? newImage)
        {
            if (ModelState.IsValid)
            {
                var existingCustomer = await _customerService.GetCustomerAsync(customer.PartitionKey!, customer.RowKey!);
                if (existingCustomer == null)
                {
                    return NotFound();
                }
                existingCustomer.FirstName = customer.FirstName;
                existingCustomer.LastName = customer.LastName;
                existingCustomer.Email = customer.Email;
                existingCustomer.PhoneNumber = customer.PhoneNumber;
                // If a new image is uploaded, replace the old one
                if (newImage != null)
                {
                    using var stream = newImage.OpenReadStream();
                    await _customerService.UpdateCustomerAsync(existingCustomer, stream, Guid.NewGuid().ToString());
                }
                else
                {
                    await _customerService.UpdateCustomerAsync(existingCustomer);
                }

                var message = new
                {
                    Action = "Customer Updated",
                    TimeStamp = DateTime.UtcNow,
                    Details = new
                    {
                        customer.PartitionKey,
                        customer.RowKey,
                        customer.FirstName,
                        customer.LastName,
                        customer.Email,
                        customer.PhoneNumber
                    }
                };
                await _queueStorageService.SendMessagesAsync(message);

                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        //Get Customer/Delete
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        //Post Customer/Delete

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            // If the customer has a photo, delete it from Blob Storage
            if (!string.IsNullOrEmpty(customer.PhotoUrl))
            {
                await _blobStorageService.DeletePhotoAsync(customer.PhotoUrl);
            }
            await _customerService.DeleteCustomerAsync(partitionKey, rowKey);

            var message = new
            {
                Action = "Customer Deleted",
                TimeStamp = DateTime.UtcNow,
                Details = new
                {
                    customer.PartitionKey,
                    customer.RowKey,
                    customer.FirstName,
                    customer.LastName,
                    customer.Email,
                    customer.PhoneNumber
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
                foreach(var log in logMessages) 
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
