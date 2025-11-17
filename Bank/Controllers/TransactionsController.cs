// Controllers/TransactionsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bank.Data;
using Bank.Models;
using System.Threading.Tasks;

namespace Bank.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows the main transaction type menu (Deposit, Withdrawal, etc.)
        public IActionResult Index()
        {
            return View();
        }

        // Shows the list of accounts to choose from for a transaction
        public async Task<IActionResult> SelectAccount(string transactionType)
        {
            if (string.IsNullOrEmpty(transactionType))
            {
                return BadRequest();
            }

            ViewData["TransactionType"] = transactionType;
            var accounts = await _context.Accounts.Include(a => a.Customer).ToListAsync();
            return View(accounts);
        }

        // GET: Prepares the deposit form
        public IActionResult CreateDeposit(int accountId)
        {
            var transaction = new Transaction { AccountId = accountId, Type = TransactionType.Deposit };
            return View(transaction);
        }

        // POST: Processes the deposit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeposit([Bind("AccountId,Amount")] Transaction transaction)
        {
            transaction.Type = TransactionType.Deposit;

            // ModelState.IsValid now works because the Transaction.Account property is nullable.
            if (ModelState.IsValid)
            {
                var account = await _context.Accounts.FindAsync(transaction.AccountId);
                if (account == null) return NotFound();

                // 1. Update the account balance
                account.Balance += transaction.Amount;

                // 2. Add the new transaction record
                _context.Add(transaction);

                // 3. Save both changes to the database
                await _context.SaveChangesAsync();
                
                return RedirectToAction("Details", "Customers", new { id = account.CustomerId });
            }
            
            // If validation fails, return to the form with error messages
            return View(transaction);
        }

        // GET: Prepares the withdrawal form
        public IActionResult CreateWithdrawal(int accountId)
        {
            var transaction = new Transaction { AccountId = accountId, Type = TransactionType.Withdrawal };
            return View(transaction);
        }

        // POST: Processes the withdrawal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWithdrawal([Bind("AccountId,Amount")] Transaction transaction)
        {
            transaction.Type = TransactionType.Withdrawal;

            if (ModelState.IsValid)
            {
                var account = await _context.Accounts.FindAsync(transaction.AccountId);
                if (account == null) return NotFound();

                // Robust check for sufficient funds, inspired by BlinkBank
                if (account.Balance < transaction.Amount)
                {
                    ModelState.AddModelError("Amount", "Insufficient funds for this withdrawal.");
                    return View(transaction); // Return to form with specific error
                }

                // 1. Update the account balance
                account.Balance -= transaction.Amount;

                // 2. Add the new transaction record
                _context.Add(transaction);

                // 3. Save both changes
                await _context.SaveChangesAsync();
                
                return RedirectToAction("Details", "Customers", new { id = account.CustomerId });
            }
            
            return View(transaction);
        }
    }
}
