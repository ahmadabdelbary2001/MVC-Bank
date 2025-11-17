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
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Accounts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Accounts.Include(a => a.Customer);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Accounts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Accounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // GET: Accounts/Create
        public IActionResult Create(int customerId)
        {
            if (customerId == 0)
            {
                return NotFound(); // Ensure a customerId is provided
            }

            // We create a temporary account object just to pass the CustomerId to the view
            var account = new Account
            {
                CustomerId = customerId
            };

            return View(account);
        }

        // POST: Accounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int customerId, decimal balance)
        {
            if (ModelState.IsValid)
            {
                // **THE DEFINITIVE FIX IS HERE**
                // 1. Manually create a new Account instance.
                //    This ensures its constructor runs and generates the AccountNumber.
                var newAccount = new Account();

                // 2. Manually assign the properties from the form.
                newAccount.CustomerId = customerId;
                newAccount.Balance = balance;

                // 3. Add the fully constructed, correct object to the context.
                _context.Add(newAccount);
                await _context.SaveChangesAsync();

                // 4. Redirect back to the customer's details page.
                return RedirectToAction("Details", "Customers", new { id = customerId });
            }

            // If model state is invalid, return to the form.
            // We need to re-pass the customerId to the view.
            var accountForView = new Account { CustomerId = customerId, Balance = balance };
            return View(accountForView);
        }
        
        // GET: Accounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Email", account.CustomerId);
            return View(account);
        }

        // POST: Accounts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Balance")] Account formData)
        {
            // 1. Fetch the original, trusted account from the database.
            var accountToUpdate = await _context.Accounts.FindAsync(id);

            if (accountToUpdate == null)
            {
                return NotFound();
            }

            // 2. Update ONLY the properties that are allowed to be changed.
            //    We bind only to "Balance" for security.
            accountToUpdate.Balance = formData.Balance;

            // 3. Manually trigger validation for the updated property if needed.
            //    For a simple balance update, this is often sufficient.
            if (await TryUpdateModelAsync(accountToUpdate, "", a => a.Balance))
            {
                try
                {
                    // 4. Save the changes. EF knows which property was modified.
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Unable to save changes. " +
                                                 "The account was modified by another user.");
                }
            }
    
            // If we get here, something failed, redisplay form.
            return View(accountToUpdate);
        }

        // GET: Accounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Accounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: Accounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account != null)
            {
                _context.Accounts.Remove(account);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AccountExists(int id)
        {
            return _context.Accounts.Any(e => e.Id == id);
        }
    }
}
