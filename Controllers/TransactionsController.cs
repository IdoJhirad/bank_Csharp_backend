using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BankBackend.Data;
using BankBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All endpoints here require a valid JWT
    public class TransactionsController : ControllerBase
    {
        private readonly BankContex _context;

        public TransactionsController(BankContex context)
        {
            _context = context;
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] TransactionDto model)
        {
            if (model.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");
            //get the id
            /*User is a ClaimsPrincipal that holds the claims from the JWT once the user is authenticated.
            FindFirstValue(ClaimTypes.NameIdentifier) looks for a claim of type "nameidentifier" 
            (often the sub claim in JWT) and returns its value.*/
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("User not found in token.");

            //find the user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return BadRequest("User does not exist.");
            }
            user.Balance += model.Amount;
            var transaction = new Transaction
            {
                Sender = user,
                Receiver = user,
                SenderName = user.Name,
                ReceiverName = user.Name,
                Amount = model.Amount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Timestamp = DateTime.UtcNow,
            };
            _context.Transactions.Add(transaction);
            //TODO add exseption handling
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Deposit successful",
                balance = user.Balance,
                transaction = new
                {
                    SenderName = user.Name,
                    ReceiverName = user.Name,
                    model.Amount,
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Completed,
                    Timestamp = DateTime.UtcNow,
                }
            });
        }


        [HttpPost("withdrawal")]
        public async Task<IActionResult> Withdraw([FromBody] TransactionDto model)
        {
            if(model.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized("User not found in token.");
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return BadRequest("User does not exist.");
            }

            if (user.Balance < model.Amount)
            {
                return BadRequest("Insufficient balance.");
            }
            user.Balance -= model.Amount;
            var transaction = new Transaction
            {
                Sender = user,
                Receiver = user,
                SenderName = user.Name,
                ReceiverName = user.Name,
                Amount = model.Amount,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed,
                Timestamp = DateTime.UtcNow,
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Withdrawal successful",
                balance = user.Balance,
                transaction = new
                {
                    SenderName = user.Name,
                    ReceiverName = user.Name,
                    model.Amount,
                    Type = TransactionType.Withdrawal,
                    Status = TransactionStatus.Completed,
                    Timestamp = DateTime.UtcNow,
                }
            });
        }
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto model)
        {
            //start transaction(session)
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (senderId == null)
                    return Unauthorized("Sender not in token.");

                var receiverEmail = model.ReceiverEmail;
                if (model.Amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero.");
                }

                //load sender
                var sender = await _context.Users
                   .FirstOrDefaultAsync(u => u.Id == senderId);
                if (sender == null)
                {
                    return NotFound("Sender not found.");
                }
                if (sender.Email == receiverEmail)
                {
                    return BadRequest("Cannot transfer to your own account.");
                }
                var receiver = await _context.Users
                  .FirstOrDefaultAsync(u => u.Email == receiverEmail);
                if (receiver == null)
                {
                    return NotFound("Receiver not found.");
                }
                if (sender.Balance < model.Amount)
                {
                    return BadRequest("Insufficient balance.");
                }
                sender.Balance -= model.Amount;
                receiver.Balance += model.Amount;
                await _context.SaveChangesAsync();

                var transactionRecord = new Transaction
                {
                    Sender = sender,
                    Receiver = receiver,
                    SenderName = sender.Name,
                    ReceiverName = receiver.Name,
                    Amount = model.Amount,
                    Type = TransactionType.Transfer,
                    Status = TransactionStatus.Completed,
                    Timestamp = DateTime.UtcNow,
                };
                _context.Transactions.Add(transactionRecord);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Transfer successful",
                    balance = sender.Balance,
                    transaction = new
                    {
                        SenderName = sender.Name,
                        ReceiverName = receiver.Name,
                        model.Amount,
                        Type = TransactionType.Transfer,
                        Status = TransactionStatus.Completed,
                        Timestamp = DateTime.UtcNow,
                    }
                });
            }
            catch (Exception ex)
            {
                // If anything fails, roll back
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = ex.Message });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized("User not found in token.");
            var transactions = await _context.Transactions
               .Where(t => t.SenderId == userId || t.ReceiverId == userId)
               .OrderByDescending(t => t.Timestamp)
               .ToListAsync();
            var resp = transactions.Select(t =>
            new
            {
                SenderName = t.SenderName,
                ReceiverName = t.ReceiverName,
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                Timestamp = t.Timestamp,
            });
            return Ok(new{ transactions = resp});
        }

    }
    public class TransactionDto
    {
        [Required]
        public decimal Amount { get; set; }
    }
    public class TransferDto
    {
        [Required]
        public string ReceiverEmail { get; set; }
        [Required]
        public decimal Amount { get; set; }
    }
}
