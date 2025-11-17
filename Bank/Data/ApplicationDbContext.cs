using Bank.Models;
using Microsoft.EntityFrameworkCore;

namespace Bank.Data;

public class ApplicationDbContext : DbContext
{
    // This constructor allows the database connection options to be passed in from Program.cs
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // These DbSet properties tell Entity Framework Core to create tables for these models.
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
}