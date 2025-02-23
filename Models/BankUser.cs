using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BankBackend.Models
{
    // BankUser inherits from IdentityUser, giving us built-in identity fields:
    // Id (string), UserName, Email, PasswordHash
    public class BankUser :IdentityUser
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Balance { get; set; } = 0M;

    }
}
