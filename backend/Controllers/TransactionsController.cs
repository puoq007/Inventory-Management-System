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

        /// <summary>ดึงรายการธุรกรรมแบบแบ่งหน้า (Pagination) พร้อมข้อมูลตำแหน่งก่อนหน้าจาก Snapshot</summary>
        [HttpGet]
        public async Task<ActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Transactions.OrderByDescending(t => t.Timestamp);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // ดึง Snapshot เพื่อเอาตำแหน่งก่อนหน้า (Source) เป็น Dictionary<TransactionId, PreviousLocatorId>
            var txnIds = items.Select(t => t.Id).ToList();
            var snapshots = await _context.JigStateSnapshots
                .Where(s => txnIds.Contains(s.TransactionId))
                .ToListAsync();

            var sources = snapshots.ToDictionary(s => s.TransactionId, s => s.PreviousLocatorId ?? "");

            return Ok(new { total, page, pageSize, items, sources });
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

            // บันทึก Snapshot สถานะก่อนเปลี่ยน
            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = actionType,
                Destination = request.Destination ?? "Production",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            var snapshot = new JigStateSnapshot
            {
                TransactionId = txn.Id,
                JigUid = jig.Uid,
                PreviousStatus = jig.Status,
                PreviousCondition = jig.Condition,
                PreviousLocatorId = jig.LocatorId
            };
            _context.JigStateSnapshots.Add(snapshot);

            jig.Status = "InUse";
            jig.LocatorId = string.IsNullOrEmpty(request.LocatorId) ? request.Destination : request.LocatorId; 
            jig.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = msg, actionType, jig, txnId = txn.Id });
        }

        /// <summary>ส่งจิกไปล้าง — เปลี่ยนสถานะเป็น Cleaning, สภาพ NeedsCleaning</summary>
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            // บันทึก Snapshot สถานะก่อนเปลี่ยน
            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = "CheckInToCleaning",
                Destination = "Cleaning Station",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            var snapshot = new JigStateSnapshot
            {
                TransactionId = txn.Id,
                JigUid = jig.Uid,
                PreviousStatus = jig.Status,
                PreviousCondition = jig.Condition,
                PreviousLocatorId = jig.LocatorId
            };
            _context.JigStateSnapshots.Add(snapshot);

            jig.Status = "Cleaning";
            jig.Condition = "NeedsCleaning";
            jig.LocatorId = request.LocatorId;
            jig.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Checked in to cleaning successfully", jig, txnId = txn.Id });
        }

        /// <summary>คืนจิกเข้าตู้เก็บ — เปลี่ยนสถานะเป็น Available, สภาพ Good</summary>
        [HttpPost("returntostore")]
        public async Task<IActionResult> ReturnToStore([FromBody] ScanRequest request)
        {
            var jigId = CleanAllSpaces(request.JigId)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(jigId)) return BadRequest("Jig ID is required");

            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Id == jigId);
            if (jig == null) return NotFound($"Jig '{jigId}' not found");

            // บันทึก Snapshot สถานะก่อนเปลี่ยน
            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = "ReturnToStore",
                Destination = "Storage",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            var snapshot = new JigStateSnapshot
            {
                TransactionId = txn.Id,
                JigUid = jig.Uid,
                PreviousStatus = jig.Status,
                PreviousCondition = jig.Condition,
                PreviousLocatorId = jig.LocatorId
            };
            _context.JigStateSnapshots.Add(snapshot);

            jig.Status = "Available";
            jig.Condition = "Good";
            jig.LocatorId = request.LocatorId;
            jig.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Returned to store successfully", jig, txnId = txn.Id });
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
                case "Other":
                    newStatus = "Evaluation";
                    newCondition = "Other";
                    break;
                default:
                    return BadRequest("Invalid Issue Type");
            }

            // บันทึก Snapshot สถานะก่อนเปลี่ยน
            var txn = new TransactionRow
            {
                JigUid = jig.Uid,
                Action = "ReportIssue",
                Destination = $"Issue: {request.IssueType} | Remark: {request.Remark}",
                User = request.User ?? "Unknown",
                Timestamp = DateTime.Now
            };

            var snapshot = new JigStateSnapshot
            {
                TransactionId = txn.Id,
                JigUid = jig.Uid,
                PreviousStatus = jig.Status,
                PreviousCondition = jig.Condition,
                PreviousLocatorId = jig.LocatorId
            };
            _context.JigStateSnapshots.Add(snapshot);

            jig.Status = newStatus;
            jig.Condition = newCondition;
            jig.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Issue reported successfully", jig });
        }

        /// <summary>
        /// ยกเลิกรายการธุรกรรม — กลับสถานะจิกจาก Snapshot ที่บันทึกไว้ก่อนทำรายการ
        /// อนุญาตเฉพาะรายการล่าสุดของจิกตัวนั้นเท่านั้น
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelTransaction(string id)
        {
            // 1. ค้นหา Transaction ที่ต้องการยกเลิก
            var txn = await _context.Transactions.FindAsync(id);
            if (txn == null) return NotFound("Transaction not found");

            // ไม่อนุญาตให้ยกเลิก CancelTransaction ซ้ำ
            if (txn.Action == "CancelTransaction")
                return BadRequest("ไม่สามารถยกเลิกรายการที่เป็น Cancel ได้");

            // 2. ตรวจสอบว่าเป็นรายการล่าสุดของจิกตัวนี้หรือไม่
            var latestTxn = await _context.Transactions
                .Where(t => t.JigUid == txn.JigUid && t.Action != "CancelTransaction")
                .OrderByDescending(t => t.Timestamp)
                .FirstOrDefaultAsync();

            if (latestTxn == null || latestTxn.Id != id)
                return BadRequest("สามารถยกเลิกได้เฉพาะรายการล่าสุดของจิกตัวนี้เท่านั้น");

            // 3. ค้นหาจิก
            var jig = await _context.Jigs.FirstOrDefaultAsync(j => j.Uid == txn.JigUid);
            if (jig == null) return NotFound("Jig not found");

            // 4. ค้นหา Snapshot สถานะก่อนหน้า (แม่นยำ 100%)
            var snapshot = await _context.JigStateSnapshots
                .FirstOrDefaultAsync(s => s.TransactionId == id);

            if (snapshot != null)
            {
                // กลับสถานะจาก Snapshot — ค่าตรงทุกตัว
                jig.Status = snapshot.PreviousStatus;
                jig.Condition = snapshot.PreviousCondition;
                jig.LocatorId = snapshot.PreviousLocatorId;
            }
            else
            {
                // Fallback: ไม่มี Snapshot (Transaction เก่าก่อนมีระบบ Snapshot) → กลับเป็น Default
                jig.Status = "Available";
                jig.Condition = "Good";
                jig.LocatorId = null;
            }

            jig.UpdatedAt = DateTime.UtcNow;

            // 5. สร้าง Transaction ใหม่ประเภท CancelTransaction
            var cancelTxn = new TransactionRow
            {
                JigUid = txn.JigUid,
                Action = "CancelTransaction",
                Destination = $"Cancelled: {txn.Action} → {txn.Destination}",
                User = txn.User,
                Timestamp = DateTime.Now
            };
            _context.Transactions.Add(cancelTxn);

            // 6. ลบ Transaction ที่ถูกยกเลิก + ลบ Snapshot
            _context.Transactions.Remove(txn);
            if (snapshot != null)
                _context.JigStateSnapshots.Remove(snapshot);

            await _context.SaveChangesAsync();

            return Ok(new { message = "ยกเลิกรายการสำเร็จ", jig, cancelledAction = txn.Action });
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
