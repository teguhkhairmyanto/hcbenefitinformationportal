using Microsoft.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Dynamic; // Diperlukan untuk ExpandoObject
using Meilisearch;    // Diperlukan untuk MeilisearchClient
using hc_benefit_information_portal_api.Models;

namespace hc_benefit_information_portal_api.Services
{
    public class BenefitFaqServices
    {
        private readonly IConfiguration _config;
        private readonly MeilisearchClient _meiliClient;

        public BenefitFaqServices(IConfiguration config, MeilisearchClient meiliClient)
        {
            _config = config;
            _meiliClient = meiliClient;
        }

        public async Task<List<object>> GetAllFaq()
        {
            string connStr = _config.GetConnectionString("DefaultConnection");

            var result = new List<object>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT 
                        f.id,
                        f.question,
                        f.answer,
                        b.id AS benefit_id,
                        b.title AS benefit_title
                    FROM faq f
                    LEFT JOIN benefits b 
                        ON f.benefit_id = b.id
                    ORDER BY f.sort_order
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                id = (int)reader["id"],
                                question = reader["question"]?.ToString(),
                                answer = reader["answer"]?.ToString(),
                                benefitId = reader["benefit_id"] == DBNull.Value ? null : reader["benefit_id"],
                                benefitTitle = reader["benefit_title"]?.ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }
        // --- METHOD BARU: UNTUK MENGAMBIL FAQ BERDASARKAN BENEFIT ID ---
        public async Task<List<object>> GetFaqByBenefitId(int benefitId)
        {
            string connStr = _config.GetConnectionString("DefaultConnection");
            var result = new List<object>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                // Gunakan WHERE f.benefit_id = @bId agar hasil terfilter
                string query = @"
                    SELECT 
                        f.id,
                        f.question,
                        f.answer,
                        b.id AS benefit_id,
                        b.title AS benefit_title
                    FROM faq f
                    LEFT JOIN benefits b 
                        ON f.benefit_id = b.id
                    WHERE f.benefit_id = @bId
                    ORDER BY f.id ASC
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Masukkan parameter benefitId ke dalam query SQL
                    cmd.Parameters.AddWithValue("@bId", benefitId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                id = (int)reader["id"],
                                question = reader["question"]?.ToString(),
                                answer = reader["answer"]?.ToString(),
                                benefitId = reader["benefit_id"] == DBNull.Value ? null : reader["benefit_id"],
                                benefitTitle = reader["benefit_title"]?.ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }

    public async Task<bool> UpdateFaqByBenefitId(int benefitId, List<FaqDto> faqs)
    {
    string connStr = _config.GetConnectionString("DefaultConnection");

    using (SqlConnection conn = new SqlConnection(connStr))
    {
        await conn.OpenAsync();
        using (SqlTransaction trans = conn.BeginTransaction())
        {
            try
            {
                // 1. Hapus FAQ lama agar tidak ada sampah data
                string deleteQuery = "DELETE FROM faq WHERE benefit_id = @bId";
                using (SqlCommand cmdDel = new SqlCommand(deleteQuery, conn, trans))
                {
                    cmdDel.Parameters.AddWithValue("@bId", benefitId);
                    await cmdDel.ExecuteNonQueryAsync();
                }

                // 2. Insert FAQ baru jika list tidak kosong
                if (faqs != null && faqs.Count > 0)
                {
                    string insertQuery = @"INSERT INTO faq (benefit_id, question, answer) 
                                         VALUES (@bId, @q, @a)";
                    foreach (var faq in faqs)
                    {
                        using (SqlCommand cmdIns = new SqlCommand(insertQuery, conn, trans))
                        {
                            cmdIns.Parameters.AddWithValue("@bId", benefitId);
                            cmdIns.Parameters.AddWithValue("@q", faq.Question); // Sesuai properti DTO Anda
                            cmdIns.Parameters.AddWithValue("@a", faq.Answer);   // Sesuai properti DTO Anda
                            await cmdIns.ExecuteNonQueryAsync();
                        }
                    }
                }

                trans.Commit();

                // 3. Sinkronisasi ke Meilisearch (Gunakan method yang sudah Anda buat sebelumnya)
                await SyncFaqsToMeilisearch(benefitId);
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                Console.WriteLine($"Error Update FAQ: {ex.Message}");
                return false;
            }
        }
    }}

        // --- METHOD BARU: UNTUK SINKRONISASI KE MEILISEARCH ---
        
        public async Task SyncFaqsToMeilisearch(int benefitId)
        {
            try
            {
                string connStr = _config.GetConnectionString("DefaultConnection");
                var newFaqs = new List<ExpandoObject>();

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    
                    // Ambil FAQ terbaru khusus untuk benefit_id yang baru di-input
                    string query = "SELECT id, benefit_id, question, answer FROM faq WHERE benefit_id = @bId";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@bId", benefitId);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                dynamic f = new ExpandoObject();
                                var fDict = (IDictionary<string, object>)f;
                                
                                fDict["id"] = reader["id"];
                                fDict["benefitId"] = reader["benefit_id"];
                                fDict["question"] = reader["question"]?.ToString() ?? "";
                                fDict["answer"] = reader["answer"]?.ToString() ?? "";
                                
                                newFaqs.Add(f);
                            }
                        }
                    }
                }
                

                // Kirim ke Meilisearch jika ada data FAQ
                if (newFaqs.Any())
                {
                    var faqIndex = _meiliClient.Index("faqs");
                    await faqIndex.AddDocumentsAsync(newFaqs);
                    Console.WriteLine($"[Meilisearch] Berhasil sinkronisasi {newFaqs.Count} FAQ untuk Benefit ID: {benefitId}");
                }
            }
            catch (Exception ex)
            {
                // Kita log error tapi tidak menghentikan proses utama API
                Console.WriteLine($"[Meilisearch Error] Gagal sinkronisasi FAQ: {ex.Message}");
            }
        }
    }
}