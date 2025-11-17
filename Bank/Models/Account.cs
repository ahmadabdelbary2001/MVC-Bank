using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bank.Models;

public class Account
{
    [Key]
    public int Id { get; set; }

    // Foreign key relationship to the Customer model
    [Required]
    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }

    [Required(ErrorMessage = "Account number is required.")]
    [StringLength(12, ErrorMessage = "Account number must be 12 digits long.", MinimumLength = 12)]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; private set; }

    [Required(ErrorMessage = "Initial balance is required.")]
    [Range(10.00, double.MaxValue, ErrorMessage = "Initial balance must be at least $10.")]
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Initial Balance")]
    public decimal Balance { get; set; } // Renamed from InitialBalance for clarity
    
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    
    public Account()
    {
        AccountNumber = GenerateRandomAccountNumber();
    }
    
    // Generates a random 12-character account number ('B' + 11 digits).
    // returns a string representing the new account number.
    private static string GenerateRandomAccountNumber()
    {
        var random = new Random();
        var stringBuilder = new StringBuilder("B", 12); // Initialize with 'B' and set capacity

        for (int i = 0; i < 11; i++)
        {
            stringBuilder.Append(random.Next(0, 10));
        }

        return stringBuilder.ToString();
    }
}