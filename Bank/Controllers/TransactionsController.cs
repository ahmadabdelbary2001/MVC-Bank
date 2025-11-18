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
        
        // GET: /Transactions/CreateTransfer?accountId=5
        // Prepares the transfer form without a ViewModel.
        public async Task<IActionResult> CreateTransfer(int accountId)
        {
            var sourceAccount = await _context.Accounts.FindAsync(accountId);
            if (sourceAccount == null)
            {
                return NotFound();
            }

            // Pass the source account's data to the view using ViewData or ViewBag.
            ViewData["SourceAccountNumber"] = sourceAccount.AccountNumber;
            ViewData["SourceAccountBalance"] = sourceAccount.Balance.ToString("C");
            ViewData["SourceAccountId"] = sourceAccount.Id; // Pass the ID for the form action
            ViewData["CustomerId"] = sourceAccount.CustomerId; // For the "Back" link

            return View();
        }

        // POST: /Transactions/CreateTransfer
        // Accepts simple parameters directly from the form.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransfer(int sourceAccountId, string destinationAccountNumber, decimal amount)
        {
            // 1. Perform validation manually.
            if (amount <= 0)
            {
                ModelState.AddModelError("Amount", "Transfer amount must be positive.");
            }
            if (string.IsNullOrWhiteSpace(destinationAccountNumber) || destinationAccountNumber.Length != 12)
            {
                ModelState.AddModelError("DestinationAccountNumber", "Destination account number must be 12 digits long.");
            }

            if (ModelState.IsValid)
            {
                // 2. Find both accounts.
                var sourceAccount = await _context.Accounts.FindAsync(sourceAccountId);
                var destinationAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountNumber == destinationAccountNumber);

                // 3. Perform business logic validation.
                if (sourceAccount == null)
                {
                    ModelState.AddModelError("", "Source account could not be found.");
                }
                else if (destinationAccount == null)
                {
                    ModelState.AddModelError("DestinationAccountNumber", "Destination account number not found.");
                }
                else if (sourceAccount.Id == destinationAccount.Id)
                {
                    ModelState.AddModelError("DestinationAccountNumber", "Cannot transfer funds to the same account.");
                }
                else if (sourceAccount.Balance < amount)
                {
                    ModelState.AddModelError("Amount", "Insufficient funds for this transfer.");
                }
                else
                {
                    // 4. All checks passed. Perform the transaction.
                    sourceAccount.Balance -= amount;
                    destinationAccount.Balance += amount;

                    var withdrawalTransaction = new Transaction
                    {
                        AccountId = sourceAccount.Id,
                        Type = TransactionType.Transfer,
                        Amount = amount,
                        DestinationAccountId = destinationAccount.Id
                    };

                    var depositTransaction = new Transaction
                    {
                        AccountId = destinationAccount.Id,
                        Type = TransactionType.Transfer,
                        Amount = amount
                    };

                    _context.Transactions.Add(withdrawalTransaction);
                    _context.Transactions.Add(depositTransaction);

                    await _context.SaveChangesAsync();

                    return RedirectToAction("Details", "Customers", new { id = sourceAccount.CustomerId });
                }
            }

            // If validation fails, repopulate ViewData and return to the form.
            var sourceAccountForView = await _context.Accounts.FindAsync(sourceAccountId);
            if (sourceAccountForView != null)
            {
                ViewData["SourceAccountNumber"] = sourceAccountForView.AccountNumber;
                ViewData["SourceAccountBalance"] = sourceAccountForView.Balance.ToString("C");
                ViewData["SourceAccountId"] = sourceAccountForView.Id;
                ViewData["CustomerId"] = sourceAccountForView.CustomerId;
            }
            
            // Pass the user's entered values back to the form so they don't have to re-type.
            ViewData["EnteredDestinationAccount"] = destinationAccountNumber;
            ViewData["EnteredAmount"] = amount;

            return View();
        }

    }
}
