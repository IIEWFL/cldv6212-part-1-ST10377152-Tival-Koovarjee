using ABC_Retail.Services.Storage;
using ABC_Retail.Services;
namespace ABC_Retail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Attribution: Microsoft. (2025). Azure Tables client library for .NET. Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet (Accessed: 28 August 2025).
            // Attribution: Microsoft. (2025). Get started with Azure Blob Storage and .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started (Accessed: 28 August 2025).
            // Attribution: Microsoft. (2024). Tutorial: Work with Azure Queue Storage queues in .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues (Accessed: 28 August 2025).
            // Attribution: Microsoft. (2025). Develop for Azure Files with .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/files/storage-dotnet-how-to-use-files (Accessed: 28 August 2025).
            // Attribution: Microsoft. (2025). Deploy ASP.NET Core apps to Azure App Service. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/azure-apps/?view=aspnetcore-9.0 (Accessed: 28 August 2025).
            // Attribution: W3Schools. (n.d.). C# Tutorial. Available at: https://www.w3schools.com/cs/index.php (Accessed: 28 August 2025).
            // Attribution: W3Schools. (n.d.). ASP.NET Razor C# Syntax. Available at: https://www.w3schools.com/asp/razor_syntax.asp (Accessed: 28 August 2025).

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var storageConnectionString = builder.Configuration.GetConnectionString("storageConnectionString")
            ?? throw new InvalidOperationException("Storage connection string is missing");

            var customerblobService = new BlobStorageService(storageConnectionString, "customer-photos");
            builder.Services.AddSingleton(customerblobService);

            builder.Services.AddSingleton(new CustomerService(storageConnectionString, "Customer", customerblobService));

            var productblobService = new BlobStorageService(storageConnectionString, "product-photos");
            builder.Services.AddSingleton(productblobService);

            builder.Services.AddSingleton(new ProductService(storageConnectionString, "Product", productblobService));

            builder.Services.AddSingleton(new BlobStorageService(storageConnectionString, "productimages"));
            builder.Services.AddSingleton(new QueueStorageService(storageConnectionString, "orderqueue-messages"));
            builder.Services.AddSingleton(new FileShareStorageService(storageConnectionString, "retail-log-file"));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
