using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Dapper; // Pastikan sudah install package Dapper lewat NuGet
using YourProject.Models;

[ApiController]
[Route("api/chat-log")]
public class ChatLogController : ControllerBase
{
    private readonly IConfiguration _config;

    public ChatLogController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SaveLog([FromBody] ChatLogRequest req)
    {
        // Identitas diambil dari sesi login (klaim JWT), BUKAN dari input frontend
        var nrp = User.FindFirst("nrp")?.Value;
        var email = User.FindFirst("email")?.Value;

        var connectionString = _config.GetConnectionString("DefaultConnection");

        using (var conn = new SqlConnection(connectionString))
        {
            // Query untuk simpan data ke tabel chat_log yang Anda buat tadi
            var query = @"
                INSERT INTO chat_log (nrp, email, question, answer_preview, status, is_emailed, created_at) 
                VALUES (@Nrp, @Email, @Question, @AnswerPreview, @Status, 0, GETDATE())";

            try 
            {
                await conn.ExecuteAsync(query, new { 
                    Nrp = nrp,
                    Email = email, 
                    req.Question, 
                    req.AnswerPreview, 
                    req.Status 
                });
                return Ok(new { message = "Log Berhasil Disimpan" });
            }
            catch (Exception ex)
            {
                // Jika error, admin bisa melihatnya di log server
                return StatusCode(500, $"Gagal simpan log: {ex.Message}");
            }
        }
    }
    // 1. ENDPOINT UNTUK MENGAMBIL DAFTAR PERTANYAAN UNANSWERED
    [HttpGet("unanswered")]
    public async Task<IActionResult> GetUnanswered()
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");

        using (var conn = new SqlConnection(connectionString))
        {
            // Kita ambil pertanyaan yang statusnya unanswered dan belum pernah dikirim email
            var query = @"
                SELECT id, nrp, email, question, answer_preview as AnswerPreview, status, FORMAT(created_at, 'dd-MM-yy HH:mm') as CreatedAt
                FROM chat_log 
                WHERE status = 'unanswered' AND is_emailed = 0 
                ORDER BY created_at DESC";

            try
            {
                var logs = await conn.QueryAsync(query);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gagal mengambil data: {ex.Message}");
            }
        }
    }

    // 2. ENDPOINT UNTUK UPDATE STATUS (TANDAI SELESAI)
    [HttpPut("mark-answered/{id}")]
    public async Task<IActionResult> MarkAnswered(int id)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");

        using (var conn = new SqlConnection(connectionString))
        {
            var query = @"
                UPDATE chat_log 
                SET status = 'answered', is_emailed = 1 
                WHERE id = @Id";

            try
            {
                var affectedRows = await conn.ExecuteAsync(query, new { Id = id });
                
                if (affectedRows > 0)
                    return Ok(new { message = "Status berhasil diperbarui" });
                
                return NotFound(new { message = "Data tidak ditemukan" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gagal update status: {ex.Message}");
            }
        }
    }
}