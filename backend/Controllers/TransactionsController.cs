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
        
        public class CheckoutRequest
        {
            public string JigId { get; set; } = "";
            public string User { get; set; } = "";
            public string Destination { get; set; } = "";
        }
    }
}
