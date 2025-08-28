using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Products : ITableEntity
    {
        public string? PartitionKey { get; set; }

        public string? RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string? ProductId { get; set; }

        public string? PhotoUrl { get; set; }

        public string? ProductName { get; set; }

        public string? Description { get; set; }

        public double Price { get; set; }

        public int Stock { get; set; }

    }
}
