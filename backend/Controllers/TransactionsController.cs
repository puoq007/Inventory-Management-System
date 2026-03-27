using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using shared.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionRow>>> GetTransactions()
        {
            return await _context.Transactions.OrderByDescending(t => t.Timestamp).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<TransactionRow>> PostTransaction(TransactionRow transaction)
        {
            SanitizeTransaction(transaction);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTransactions), new { id = transaction.Id }, transaction);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(string id)
        {
            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound();
            _context.Transactions.Remove(txn);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            if (jig.Status == "InUse") return BadRequest("Jig is already checked out");

            jig.Status = "InUse";
            jig.LocatorId = request.Destination; // Store production zone name
            jig.UpdatedAt = DateTime.UtcNow;

            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = "CheckOut",
                Destination = request.Destination ?? "Production",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Checked out successfully", jig });
        }

        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            jig.Status = "Cleaning";
            jig.Condition = "NeedsCleaning";
            jig.LocatorId = request.LocatorId; // Cleaning station ID
            jig.UpdatedAt = DateTime.UtcNow;

            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = "CheckInToCleaning",
                Destination = "Cleaning Station",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Checked in to cleaning successfully", jig });
        }

        [HttpPost("returntostore")]
        public async Task<IActionResult> ReturnToStore([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            jig.Status = "Available";
            jig.Condition = "Good";
            jig.LocatorId = request.LocatorId; // Storage locator ID
            jig.UpdatedAt = DateTime.UtcNow;

            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = "ReturnToStore",
                Destination = "Storage",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Returned to store successfully", jig });
        }

        public class ScanRequest
        {
            public string JigId { get; set; } = "";
            public string User { get; set; } = "";
            public string? Destination { get; set; }
            public string? LocatorId { get; set; }
        }

        private void SanitizeTransaction(TransactionRow txn)
        {
            if (txn == null) return;
            txn.User = NormalizeSpaces(txn.User) ?? "Unknown";
            txn.Action = NormalizeSpaces(txn.Action) ?? "Unknown";
            txn.Destination = NormalizeSpaces(txn.Destination) ?? "";
            txn.JigUid = CleanAllSpaces(txn.JigUid) ?? "";
        }

        private string? CleanAllSpaces(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
            return new string(val.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        private string? NormalizeSpaces(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
            return System.Text.RegularExpressions.Regex.Replace(val.Trim(), @"\s+", " ");
        }
    }
}
