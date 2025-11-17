// Models/Transaction.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models;

// Enum to define the types of transactions
public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer
}

public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AccountId { get; set; } // The account this transaction belongs to

    [ForeignKey("AccountId")]
    public virtual Account Account { get; set; }

    [Required]
    public TransactionType Type { get; set; } // Deposit, Withdrawal, or Transfer

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Transaction amount must be positive.")]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // For transfers, this records the destination. Can be null for deposits/withdrawals.
    public int? DestinationAccountId { get; set; }
}