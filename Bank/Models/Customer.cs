using System.ComponentModel.DataAnnotations;

namespace Bank.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(100)]
    public string Email { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; } // Optional phone number

    [Display(Name = "Date Joined")]
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    // Navigation property for the accounts associated with this customer.
    // A customer can have multiple accounts.
    // A customer can have only one account.
    public virtual Account? Account { get; set; }
}