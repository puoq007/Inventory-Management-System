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
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTransactions), new { id = transaction.Id }, transaction);
        }

        [HttpPost("checkout")]
        public async Task<ActionResult> CheckOut([FromBody] CheckoutRequest req)
        {
            var jig = await _context.PhysicalJigs.FindAsync(req.JigId);
            if (jig == null) return NotFound("Jig not found.");
            if (jig.Status == "InUse") return BadRequest("Jig is already checked out.");

            jig.Status = "InUse";
            jig.CurrentDestination = req.Destination;

            var transaction = new TransactionRow
            {
                JigId = req.JigId,
                Action = "CheckOut",
                Destination = req.Destination,
                User = req.User,
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(transaction);
        }
        
        [HttpPost("checkin")]
        public async Task<ActionResult> CheckIn([FromBody] CheckinRequest req)
        {
            var jig = await _context.PhysicalJigs.FindAsync(req.JigId);
            if (jig == null) return NotFound("Jig not found.");
            if (jig.Status != "InUse") return BadRequest("Jig is not currently checked out.");

            if (string.IsNullOrEmpty(req.LocatorId))
                return BadRequest("Destination locator is required.");

            var cleaningLoc = await _context.Locators.FindAsync(req.LocatorId);
            if (cleaningLoc == null)
                return BadRequest($"Invalid cleaning destination: Locator '{req.LocatorId}' not found in database.");
            if (cleaningLoc.Type != "Cleaning")
                return BadRequest($"Invalid cleaning destination: Locator '{req.LocatorId}' has type '{cleaningLoc.Type}' instead of 'Cleaning'.");

            jig.Status = "Cleaning";
            jig.CurrentDestination = "";
            jig.LocatorId = req.LocatorId;

            var transaction = new TransactionRow
            {
                JigId = req.JigId,
                Action = "CheckInToCleaning",
                Destination = req.LocatorId,
                User = req.User,
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(transaction);
        }

        [HttpPost("returntostore")]
        public async Task<ActionResult> ReturnToStore([FromBody] ReturnToStoreRequest req)
        {
            var jig = await _context.PhysicalJigs.FindAsync(req.JigId);
            if (jig == null) return NotFound("Jig not found.");
            if (jig.Status != "Cleaning" && jig.Status != "InUse") return BadRequest("Jig must be In Use or in Cleaning to be stored.");

            if (string.IsNullOrEmpty(req.LocatorId))
                return BadRequest("Destination locator is required.");

            var storeLoc = await _context.Locators.FindAsync(req.LocatorId);
            if (storeLoc == null)
                return BadRequest($"Invalid storage destination: Locator '{req.LocatorId}' not found in database.");
            if (storeLoc.Type == "Production" || storeLoc.Type == "Cleaning")
                return BadRequest($"Invalid storage destination: Locator '{req.LocatorId}' is a {storeLoc.Type} zone, not a storage location.");

            jig.Status = "Available";
            jig.LocatorId = req.LocatorId;
            jig.HomeLocatorId = req.LocatorId; // Update home locator to wherever they put it back

            var transaction = new TransactionRow
            {
                JigId = req.JigId,
                Action = "ReturnToStore",
                Destination = req.LocatorId,
                User = req.User,
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(transaction);
        }
        
        public class CheckoutRequest
        {
            public string JigId { get; set; } = "";
            public string User { get; set; } = "";
            public string Destination { get; set; } = "";
        }

        public class CheckinRequest
        {
            public string JigId { get; set; } = "";
            public string User { get; set; } = "";
            public string LocatorId { get; set; } = "";
        }

        public class ReturnToStoreRequest
        {
            public string JigId { get; set; } = "";
            public string User { get; set; } = "";
            public string LocatorId { get; set; } = "";
        }
    }
}
