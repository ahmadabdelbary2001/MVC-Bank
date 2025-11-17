// Controllers/TransactionsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bank.Data;
using Bank.Models;
using System.Linq;
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
        
        // GET: Transactions
        public IActionResult Index()
        {
            return View();
        }
        
        // GET: Transactions/SelectAccount
        // This page allows the user to select which account to perform a transaction on.
        public async Task<IActionResult> SelectAccount(string transactionType)
        {
            if (string.IsNullOrEmpty(transactionType))
            {
                return BadRequest();
            }

            // Pass the transaction type (e.g., "Deposit") and a list of all accounts to the view.
            ViewData["TransactionType"] = transactionType;
            var accounts = await _context.Accounts.Include(a => a.Customer).ToListAsync();
    
            return View(accounts);
        }

        // GET: Transactions/CreateDeposit
        public IActionResult CreateDeposit(int accountId)
        {
            var transaction = new Transaction { AccountId = accountId, Type = TransactionType.Deposit };
            return View(transaction);
        }

        // POST: Transactions/CreateDeposit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeposit([Bind("AccountId,Amount")] Transaction transaction)
        {
            transaction.Type = TransactionType.Deposit;
            if (ModelState.IsValid)
            {
                var account = await _context.Accounts.FindAsync(transaction.AccountId);
                if (account == null) return NotFound();

                // Update account balance
                account.Balance += transaction.Amount;

                // Record the transaction
                _context.Add(transaction);
                _context.Update(account);

                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Customers", new { id = account.CustomerId });
            }
            return View(transaction);
        }

        // GET: Transactions/CreateWithdrawal
        public IActionResult CreateWithdrawal(int accountId)
        {
            var transaction = new Transaction { AccountId = accountId, Type = TransactionType.Withdrawal };
            return View(transaction);
        }

        // POST: Transactions/CreateWithdrawal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWithdrawal([Bind("AccountId,Amount")] Transaction transaction)
        {
            transaction.Type = TransactionType.Withdrawal;
            if (ModelState.IsValid)
            {
                var account = await _context.Accounts.FindAsync(transaction.AccountId);
                if (account == null) return NotFound();

                // Check for sufficient funds
                if (account.Balance < transaction.Amount)
                {
                    ModelState.AddModelError("Amount", "Insufficient funds for this withdrawal.");
                    return View(transaction);
                }

                // Update account balance
                account.Balance -= transaction.Amount;

                // Record the transaction
                _context.Add(transaction);
                _context.Update(account);

                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Customers", new { id = account.CustomerId });
            }
            return View(transaction);
        }
    }
}
