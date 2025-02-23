using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankBackend.Models
{
    public class Transaction
    {
        // Primary key
        public int Id { get; set; }
        [Required]
        public string SenderId { get; set; } = string.Empty;
        [Required]
        public string ReceiverId { get; set; } = string.Empty;
        [Required]
        public string SenderName { get; set; } = string.Empty;
        [Required]
        public BankUser Sender { get; set; } = default!;
        [Required]
        public string ReceiverName{ get; set; } = string.Empty;   
        [Required]
        public BankUser Receiver { get; set; } = default!;


        [Column(TypeName = "decimal(18, 2)")] 
        public decimal Amount { get; set; }
        public TransactionStatus Status { get; set; }

        public TransactionType Type { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}
public enum TransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    Transfer = 2
}