


using BankBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankBackend.Data
{
    // IdentityDbContext<BankUser>  map Identity tables
    public class BankContex : IdentityDbContext<BankUser>
    {
        public BankContex(DbContextOptions<BankContex> options) :base(options) { }

        public DbSet<Transaction> Transactions { get; set; }


        //value converter for the enum as string
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            //enum conversion to string
            builder.Entity<Transaction>()
            .Property(t => t.Status)
            .HasConversion<string>();

            builder.Entity<Transaction>()
           .Property(t => t.Type)
           .HasConversion<string>();
            //for not casced ad dystroy transaction at delete
            builder.Entity<Transaction>()
            .HasOne(t => t.Sender)
            .WithMany()  // or a collection if you want
            .HasForeignKey(t => t.SenderId)
            .OnDelete(DeleteBehavior.Restrict); // <-- important

            // For Transactions → Receiver
            builder.Entity<Transaction>()
                .HasOne(t => t.Receiver)
                .WithMany()  // or a collection if you want
                .HasForeignKey(t => t.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict); // or .SetNull, as you prefer

        }
    }
}
