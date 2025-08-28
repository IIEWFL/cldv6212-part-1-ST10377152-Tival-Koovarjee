namespace ABC_Retail.Models
{
    public class QueueLogViewModel
    {
        public string MessageId { get; set; }
        public DateTimeOffset InsertionTime { get; set; }
        public string MessageText { get; set; }
    }
}
