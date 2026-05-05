using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using shared.Models;

namespace backend.Controllers
{
    /// <summary>
    /// Controller จัดการธุรกรรมการใช้งานจิก — เบิก (CheckOut), คืน (ReturnToStore), ส่งล้าง (CheckIn), แจ้งปัญหา (ReportIssue)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>ดึงรายการธุรกรรมแบบแบ่งหน้า (Pagination)</summary>
        [HttpGet]
        public async Task<ActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Transactions.OrderByDescending(t => t.Timestamp);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new { total, page, pageSize, items });
        }

        /// <summary>สร้างรายการธุรกรรมโดยตรง</summary>
        [HttpPost]
        public async Task<ActionResult<TransactionRow>> PostTransaction(TransactionRow transaction)
        {
            SanitizeTransaction(transaction);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTransactions), new { id = transaction.Id }, transaction);
        }

        /// <summary>ลบรายการธุรกรรม</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(string id)
        {
            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound();
            _context.Transactions.Remove(txn);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        /// <summary>
        /// เบิกจิกออกไปสายการผลิต — ถ้าจิกอยู่ InUse อยู่แล้วจะเป็น Transfer แทน
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            if (jig.Status == "Cleaning" || jig.Status == "Evaluation" || jig.Status == "Lost")
                return BadRequest($"ไม่สามารถทำรายการได้ สถานะปัจจุบันของจิ๊กคือ '{jig.Status}'");

            string actionType = (jig.Status == "InUse") ? "Transfer" : "CheckOut";
            string msg = (actionType == "Transfer") ? "Transferred successfully" : "Checked out successfully";

            jig.Status = "InUse";
            jig.LocatorId = string.IsNullOrEmpty(request.LocatorId) ? request.Destination : request.LocatorId; 
            jig.UpdatedAt = DateTime.UtcNow;

            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = actionType,
                Destination = request.Destination ?? "Production",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = msg, actionType, jig });
        }

        /// <summary>ส่งจิกไปล้าง — เปลี่ยนสถานะเป็น Cleaning, สภาพ NeedsCleaning</summary>
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            jig.Status = "Cleaning";
            jig.Condition = "NeedsCleaning";
            jig.LocatorId = request.LocatorId; // รหัสสถานีล้าง
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

        /// <summary>คืนจิกเข้าตู้เก็บ — เปลี่ยนสถานะเป็น Available, สภาพ Good</summary>
        [HttpPost("returntostore")]
        public async Task<IActionResult> ReturnToStore([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            jig.Status = "Available";
            jig.Condition = "Good";
            jig.LocatorId = request.LocatorId; // รหัสตำแหน่งจัดเก็บ
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

        /// <summary>โมเดล Request สำหรับการสแกนเบิก/คืน/ล้าง</summary>
        public class ScanRequest
        {
            public string JigId { get; set; } = "";
            public string User { get; set; } = "";
            public string? Destination { get; set; }
            public string? LocatorId { get; set; }
        }

        /// <summary>โมเดล Request สำหรับการแจ้งปัญหา</summary>
        public class ReportIssueRequest
        {
            public string JigId { get; set; } = "";
            public string User { get; set; } = "";
            public string IssueType { get; set; } = ""; // Broken, NeedsCleaning, Lost
            public string Remark { get; set; } = "";
        }

        /// <summary>
        /// แจ้งปัญหาจิก — เปลี่ยนสถานะ/สภาพตาม IssueType (Broken, NeedsCleaning, Lost)
        /// </summary>
        [HttpPost("reportissue")]
        public async Task<IActionResult> ReportIssue([FromBody] ReportIssueRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            string newStatus;
            string newCondition;

            switch (request.IssueType)
            {
                case "Broken":
                    newStatus = "Evaluation";
                    newCondition = "Broken";
                    break;
                case "NeedsCleaning":
                    newStatus = "Cleaning";
                    newCondition = "NeedsCleaning";
                    break;
                case "Lost":
                    newStatus = "Lost";
                    newCondition = "Lost";
                    break;
                default:
                    return BadRequest("Invalid Issue Type");
            }

            jig.Status = newStatus;
            jig.Condition = newCondition;
            jig.UpdatedAt = DateTime.UtcNow;

            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = "ReportIssue",
                Destination = $"Issue: {request.IssueType} | Remark: {request.Remark}",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Issue reported successfully", jig });
        }

        /// <summary>ทำความสะอาดข้อมูลธุรกรรมก่อนบันทึก</summary>
        private void SanitizeTransaction(TransactionRow txn)
        {
            if (txn == null) return;
            txn.User = NormalizeSpaces(txn.User) ?? "Unknown";
            txn.Action = NormalizeSpaces(txn.Action) ?? "Unknown";
            txn.Destination = NormalizeSpaces(txn.Destination) ?? "";
            txn.JigUid = CleanAllSpaces(txn.JigUid) ?? "";
        }

        /// <summary>ลบ Whitespace ทั้งหมดออกจากค่า</summary>
        private string? CleanAllSpaces(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
            return new string(val.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>แปลงช่องว่างซ้ำซ้อนเป็นช่องว่างเดียว</summary>
        private string? NormalizeSpaces(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return val?.Trim();
            return System.Text.RegularExpressions.Regex.Replace(val.Trim(), @"\s+", " ");
        }
    }
}
