using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bank.Data;
using Bank.Models;

namespace Bank.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Customers.ToListAsync());
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.Account)
                    .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (customer == null)
            {
                return NotFound();
            }

            if (customer.Account != null && customer.Account.Transactions.Any())
            {
                var transactionHistoryForView = new List<dynamic>();
                var allAccountNumbers = await _context.Accounts.ToDictionaryAsync(a => a.Id, a => a.AccountNumber);

                foreach (var transaction in customer.Account.Transactions)
                {
                    string details = "-";
                    if (transaction.Type == TransactionType.Transfer)
                    {
                        // Case 1: This is a TRANSFER OUT. We know the destination.
                        if (transaction.DestinationAccountId.HasValue)
                        {
                            if (allAccountNumbers.TryGetValue(transaction.DestinationAccountId.Value, out var destNumber))
                            {
                                details = $"To: {destNumber}";
                            }
                        }
                        // Case 2: This is a TRANSFER IN. We need to find the sender.
                        else
                        {
                            // --- THIS IS THE CORRECTED, ROBUST LOGIC ---
                            // Define a small time window for the search (e.g., +/- 1 second).
                            var startTime = transaction.CreatedDate.AddSeconds(-1);
                            var endTime = transaction.CreatedDate.AddSeconds(1);

                            // Find the transaction that was SENT to THIS account within the time window.
                            var sourceTransaction = await _context.Transactions
                                .FirstOrDefaultAsync(t => 
                                    t.DestinationAccountId == transaction.AccountId && 
                                    t.Amount == transaction.Amount && 
                                    t.Type == TransactionType.Transfer &&
                                    t.CreatedDate >= startTime && t.CreatedDate <= endTime); // Use the time window

                            if (sourceTransaction != null && allAccountNumbers.TryGetValue(sourceTransaction.AccountId, out var sourceNumber))
                            {
                                details = $"From: {sourceNumber}";
                            }
                        }
                    }
                    transactionHistoryForView.Add(new { Transaction = transaction, Details = details });
                }
                ViewBag.TransactionHistory = transactionHistoryForView;
            }

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,PhoneNumber")] Customer customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    customer.DateJoined = DateTime.UtcNow;
                    _context.Add(customer);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Create", "Accounts", new { customerId = customer.Id });
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again.");
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,PhoneNumber,DateJoined")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
