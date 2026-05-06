using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using hc_benefit_information_portal_api.Models; // Pastikan namespace DTO benar
using System.Dynamic;
using Meilisearch;


namespace hc_benefit_information_portal_api.Services
{
    public class BenefitService
    {
        private readonly IConfiguration _config;
        private readonly MeilisearchClient _meiliClient;
        private readonly BenefitFaqServices _faqService;

        // Tambahkan MeilisearchClient meiliClient di parameter:
        public BenefitService(IConfiguration config, MeilisearchClient meiliClient, BenefitFaqServices faqService)
        {
            _config = config;
            _meiliClient = meiliClient;
            _faqService = faqService;
        }

        // ==========================================================
        // 🔹 BARU: METHOD UNTUK SIMPAN DATA (STEP 5)
        // ==========================================================
        public async Task<bool> CreateBenefitAsync(BenefitCreateDto dto)
        {
            string connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                // Menggunakan Transaction agar jika satu gagal, semua di-rollback
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Simpan ke tabel 'benefits'
                        int newBenefitId;
                        string queryBenefit = @"
                            INSERT INTO benefits (title, slug, description, category_id, is_active, created_at) 
                            OUTPUT INSERTED.id
                            VALUES (@title, @slug,@desc, @catId, 1, GETDATE())";

                        using (SqlCommand cmd = new SqlCommand(queryBenefit, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@title", dto.Title);
                            cmd.Parameters.AddWithValue("@slug", dto.Title.ToLower().Replace(" ", "-"));
                            cmd.Parameters.AddWithValue("@desc", dto.Description);
                            cmd.Parameters.AddWithValue("@catId", dto.Category);
                            newBenefitId = (int)await cmd.ExecuteScalarAsync();
                        }

                        // 2. Simpan ke tabel 'benefit_details' (Loop 6 Seksi)
                        if (dto.Details != null)
                        {
                            foreach (var detail in dto.Details)
                            {
                                if (!string.IsNullOrWhiteSpace(detail.Value))
                                {
                                    string queryDetail = @"
                                        INSERT INTO benefit_details (benefit_id, section_title_id, content) 
                                        VALUES (@bId, @sId, @content)";
                                    using (SqlCommand cmd = new SqlCommand(queryDetail, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@bId", newBenefitId);
                                        cmd.Parameters.AddWithValue("@sId", detail.Key);
                                        cmd.Parameters.AddWithValue("@content", detail.Value);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        // 3. Simpan ke tabel 'faq'
                        if (dto.Faqs != null)
                        {
                            foreach (var faq in dto.Faqs)
                            {
                                string queryFaq = @"
                                    INSERT INTO faq (benefit_id, question, answer) 
                                    VALUES (@bId, @q, @a)";
                                using (SqlCommand cmd = new SqlCommand(queryFaq, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@bId", newBenefitId);
                                    cmd.Parameters.AddWithValue("@q", faq.Question);
                                    cmd.Parameters.AddWithValue("@a", faq.Answer ?? (object)DBNull.Value);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // 4. Simpan Tags (Logika Junction Table)
                        if (dto.Tags != null && dto.Tags.Any())
                        {
                            int tagId;
                            foreach (var tagName in dto.Tags)
                            {
                                // A. Cek/Insert ke tabel 'tags'
                                
                                string queryCheckTag = "SELECT id FROM tags WHERE name = @tagName";
                                using (SqlCommand cmd = new SqlCommand(queryCheckTag, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@tagName", tagName);
                                    var result = await cmd.ExecuteScalarAsync();

                                    if (result != null) {
                                        tagId = (int)result;
                                    } else {
                                        string queryInsertTag = "INSERT INTO tags (name) OUTPUT INSERTED.id VALUES (@tagName)";
                                        using (SqlCommand cmdInsert = new SqlCommand(queryInsertTag, conn, trans))
                                        {
                                            cmdInsert.Parameters.AddWithValue("@tagName", tagName);
                                            tagId = (int)await cmdInsert.ExecuteScalarAsync();
                                        }
                                    }
                                }

                                // B. Simpan ke tabel penghubung 'benefit_tags'
                                string queryJunction = "INSERT INTO benefit_tags (benefit_id, tag_id) VALUES (@bId, @tId)";
                                using (SqlCommand cmdJunc = new SqlCommand(queryJunction, conn, trans))
                                {
                                    cmdJunc.Parameters.AddWithValue("@bId", newBenefitId);
                                    cmdJunc.Parameters.AddWithValue("@tId", tagId);
                                    await cmdJunc.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        trans.Commit();
                       try
                        {
                            // A. SYNC BENEFIT UTAMA
                            var rawBenefits = await GetAllBenefits(null); 
                            var newBenefitData = rawBenefits.Cast<dynamic>().FirstOrDefault(b => b.id == newBenefitId);

                            if (newBenefitData != null)
                            {
                                var cleanedBenefit = MapToMeilisearchModel(newBenefitData);
                                var index = _meiliClient.Index("benefits");
                                await index.AddDocumentsAsync(new[] { cleanedBenefit });
                                Console.WriteLine($"Incremental Sync Success: Benefit ID {newBenefitId}");
                            }

                            // B. SYNC FAQ (MENGGUNAKAN SERVICE FAQ)
                            // Baris ini menggantikan seluruh query manual FAQ sebelumnya
                            await _faqService.SyncFaqsToMeilisearch(newBenefitId);
                        }
                        catch (Exception meiliEx)
                        {
                            Console.WriteLine($"Meilisearch Error: {meiliEx.Message}");
                        }
                        
                        return true;
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        // ==========================================================
        // 🔹 BARU: METHOD UNTUK DASHBOARD COUNT
        // ==========================================================
        public async Task<List<object>> GetBenefitCountByCategory()
        {
            string connStr = _config.GetConnectionString("DefaultConnection");
            var result = new List<object>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                // Kita asumsikan ada tabel 'categories' untuk mendapatkan nama kategori
                // Jika tidak ada tabel categories, hapus bagian JOIN dan tampilkan b.category_id saja
                string query = @"
                    SELECT 
                        c.id AS category_id, 
                        COUNT(b.id) AS total_count 
                    FROM benefits b
                    LEFT JOIN categories c ON b.category_id = c.id
                    WHERE b.is_active = 1
                    GROUP BY c.id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                categoryId = reader["category_id"] == DBNull.Value ? "Uncategorized" : reader["category_id"],
                                count = (int)reader["total_count"]
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ==========================================================
        // 📄 GET ALL BENEFITS (EKSISTING)
        // ==========================================================
        public async Task<List<object>> GetAllBenefits(int? categoryId)
        {
            string connStr = _config.GetConnectionString("DefaultConnection");
            var benefitDict = new Dictionary<int, dynamic>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT 
                        b.id,
                        b.title,
                        b.description,
                        b.category_id,

                        d.id AS detail_id,
                        s.id AS section_id,
                        s.name AS section_title,
                        d.content,

                        t.name AS tag_name

                    FROM benefits b
                    LEFT JOIN benefit_details d 
                        ON b.id = d.benefit_id
                    LEFT JOIN section_titles s 
                        ON d.section_title_id = s.id
                    LEFT JOIN benefit_tags bt 
                        ON b.id = bt.benefit_id
                    LEFT JOIN tags t 
                        ON bt.tag_id = t.id
                    WHERE b.is_active = 1  -- 🔹 Kunci Soft Delete: Hanya ambil data aktif
                    AND (@categoryId IS NULL OR b.category_id = @categoryId)
                    ORDER BY b.id, s.id
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@categoryId", (object?)categoryId ?? DBNull.Value);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int benefitId = (int)reader["id"];

                            if (!benefitDict.ContainsKey(benefitId))
                            {
                                benefitDict[benefitId] = new
                                {
                                    id = benefitId,
                                    title = reader["title"]?.ToString(),
                                    description = reader["description"]?.ToString(),
                                    category = reader["category_id"],
                                    sections = new List<dynamic>(),
                                    tags = new List<string>()
                                };
                            }

                            var benefit = benefitDict[benefitId];

                            // HANDLE TAG
                            if (reader["tag_name"] != DBNull.Value)
                            {
                                var tags = (List<string>)benefit.tags;
                                string tagName = reader["tag_name"].ToString();

                                if (!tags.Contains(tagName))
                                {
                                    tags.Add(tagName);
                                }
                            }

                            if (reader["section_id"] == DBNull.Value)
                                continue;

                            int sectionId = (int)reader["section_id"];
                            string sectionTitle = reader["section_title"]?.ToString();
                            var sections = (List<dynamic>)benefit.sections;
                            var existingSection = sections.FirstOrDefault(s => s.sectionId == sectionId);

                            if (existingSection == null)
                            {
                                existingSection = new
                                {
                                    sectionId = sectionId,
                                    sectionTitle = sectionTitle,
                                    details = new List<dynamic>()
                                };
                                sections.Add(existingSection);
                            }

                            // HANDLE DETAIL
                            if (reader["detail_id"] != DBNull.Value)
                            {
                                ((List<dynamic>)existingSection.details).Add(new
                                {
                                    content = reader["content"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }

            return benefitDict.Values.ToList();
        }

        // ==========================================================
        // 🔹 TAMBAHKAN INI: METHOD UNTUK UPDATE DATA
        // ==========================================================
        // ==========================================================
// 🔹 UPDATE: STRATEGI DELETE -> INSERT (REBORN)
// ==========================================================
public async Task<bool> UpdateBenefitAsync(int id, BenefitCreateDto dto)
{
    string connStr = _config.GetConnectionString("DefaultConnection");

    using (SqlConnection conn = new SqlConnection(connStr))
    {
        await conn.OpenAsync();
        using (SqlTransaction trans = conn.BeginTransaction())
        {
            try
            {
                // 1. Update data utama di tabel 'benefits'
                string queryUpdateBenefit = @"
                    UPDATE benefits 
                    SET title = @title, 
                        slug = @slug, 
                        description = @desc, 
                        category_id = @catId, 
                        updated_at = GETDATE() 
                    WHERE id = @id AND is_active = 1";

                using (SqlCommand cmd = new SqlCommand(queryUpdateBenefit, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@title", dto.Title);
                    cmd.Parameters.AddWithValue("@slug", dto.Title.ToLower().Replace(" ", "-"));
                    cmd.Parameters.AddWithValue("@desc", dto.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@catId", dto.Category);
                    cmd.Parameters.AddWithValue("@id", id);
                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected == 0) return false; // Benefit tidak ditemukan atau sudah tidak aktif
                }

                // 2. DELETE SEMUA RELASI LAMA (Details, Tags, FAQ)
                string deleteDetails = "DELETE FROM benefit_details WHERE benefit_id = @id";
                string deleteTags = "DELETE FROM benefit_tags WHERE benefit_id = @id";
                string deleteFaqs = "DELETE FROM faq WHERE benefit_id = @id";

                using (SqlCommand cmd = new SqlCommand(deleteDetails, conn, trans)) { cmd.Parameters.AddWithValue("@id", id); await cmd.ExecuteNonQueryAsync(); }
                using (SqlCommand cmd = new SqlCommand(deleteTags, conn, trans)) { cmd.Parameters.AddWithValue("@id", id); await cmd.ExecuteNonQueryAsync(); }
                using (SqlCommand cmd = new SqlCommand(deleteFaqs, conn, trans)) { cmd.Parameters.AddWithValue("@id", id); await cmd.ExecuteNonQueryAsync(); }

                // 3. INSERT ULANG 'benefit_details'
                if (dto.Details != null)
                {
                    foreach (var detail in dto.Details)
                    {
                        if (!string.IsNullOrWhiteSpace(detail.Value))
                        {
                            string queryDetail = "INSERT INTO benefit_details (benefit_id, section_title_id, content) VALUES (@bId, @sId, @content)";
                            using (SqlCommand cmd = new SqlCommand(queryDetail, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@bId", id);
                                cmd.Parameters.AddWithValue("@sId", detail.Key);
                                cmd.Parameters.AddWithValue("@content", detail.Value);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 4. INSERT ULANG 'faq'
                if (dto.Faqs != null)
                {
                    foreach (var faq in dto.Faqs)
                    {
                        if (!string.IsNullOrWhiteSpace(faq.Question))
                        {
                            string queryFaq = "INSERT INTO faq (benefit_id, question, answer) VALUES (@bId, @q, @a)";
                            using (SqlCommand cmd = new SqlCommand(queryFaq, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@bId", id);
                                cmd.Parameters.AddWithValue("@q", faq.Question);
                                cmd.Parameters.AddWithValue("@a", faq.Answer ?? (object)DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 5. INSERT ULANG 'tags' (Logika Junction Table + Anti Duplikat)
                if (dto.Tags != null && dto.Tags.Any())
                {
                    // Menghapus duplikat dari input untuk mencegah error Primary Key/Junction
                    var uniqueTags = dto.Tags
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => t.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var tagName in uniqueTags)
                    {
                        int tagId;
                        // Cek/Insert ke tabel master 'tags'
                        string queryCheckTag = "SELECT id FROM tags WHERE name = @tagName";
                        using (SqlCommand cmd = new SqlCommand(queryCheckTag, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@tagName", tagName);
                            var result = await cmd.ExecuteScalarAsync();

                            if (result != null) {
                                tagId = (int)result;
                            } else {
                                string queryInsertTag = "INSERT INTO tags (name) OUTPUT INSERTED.id VALUES (@tagName)";
                                using (SqlCommand cmdInsert = new SqlCommand(queryInsertTag, conn, trans))
                                {
                                    cmdInsert.Parameters.AddWithValue("@tagName", tagName);
                                    tagId = (int)await cmdInsert.ExecuteScalarAsync();
                                }
                            }
                        }

                        // Simpan ke junction table 'benefit_tags'
                        string queryJunction = "INSERT INTO benefit_tags (benefit_id, tag_id) VALUES (@bId, @tId)";
                        using (SqlCommand cmdJunc = new SqlCommand(queryJunction, conn, trans))
                        {
                            cmdJunc.Parameters.AddWithValue("@bId", id);
                            cmdJunc.Parameters.AddWithValue("@tId", tagId);
                            await cmdJunc.ExecuteNonQueryAsync();
                        }
                    }
                }

                trans.Commit();

                // 6. ADAPTASI SINKRONISASI MEILISEARCH (Identik dengan CreateBenefitAsync)
                try
                {
                    // A. SYNC BENEFIT UTAMA
                    var rawBenefits = await GetAllBenefits(null); 
                    var updatedBenefitData = rawBenefits.Cast<dynamic>().FirstOrDefault(b => b.id == id);

                    if (updatedBenefitData != null)
                    {
                        var cleanedBenefit = MapToMeilisearchModel(updatedBenefitData);
                        var index = _meiliClient.Index("benefits");
                        // AddDocumentsAsync akan melakukan 'upsert' (update jika ID sudah ada)
                        await index.AddDocumentsAsync(new[] { cleanedBenefit });
                        Console.WriteLine($"Update Sync Success: Benefit ID {id}");
                    }

                    // B. SYNC FAQ (MENGGUNAKAN SERVICE FAQ)
                    await _faqService.SyncFaqsToMeilisearch(id);
                }
                catch (Exception meiliEx)
                {
                    Console.WriteLine($"Meilisearch Update Sync Error: {meiliEx.Message}");
                }

                return true;
            }
            catch (Exception)
            {
                trans.Rollback();
                throw;
            }
        }
    }
}
    

        // ==========================================================
        // 🔹 UPDATE: SOFT DELETE (Hanya ubah is_active jadi 0)
        // ==========================================================
        public async Task<bool> DeleteBenefitAsync(int id)
        {
            string connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                // Tidak perlu transaksi rumit karena hanya update 1 tabel
                string query = "UPDATE benefits SET is_active = 0, updated_at = GETDATE() WHERE id = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int affected = await cmd.ExecuteNonQueryAsync();

                    if (affected > 0)
                    {
                        // SYNC KE MEILISEARCH (Hapus dari index pencarian agar tidak bisa dicari)
                        try
                        {
                            var index = _meiliClient.Index("benefits");
                            await index.DeleteDocumentsAsync(new[] { id.ToString() });
                            Console.WriteLine($"Soft Delete Success & Meilisearch Synced: ID {id}");
                        }
                        catch (Exception meiliEx)
                        {
                            Console.WriteLine($"Meilisearch Delete Error: {meiliEx.Message}");
                        }

                        return true;
                    }
                }
            }
            return false;
        }


        private ExpandoObject MapToMeilisearchModel(dynamic benefit)
        {
            dynamic expBenefit = new ExpandoObject();
            var benefitDict = (IDictionary<string, object>)expBenefit;

            benefitDict["id"] = benefit.id;
            benefitDict["title"] = benefit.title ?? "";
            benefitDict["description"] = benefit.description ?? "";
            benefitDict["category"] = benefit.category ?? 0;

            var sections = (benefit.sections as IEnumerable<dynamic>)?.ToList() ?? new List<dynamic>();
            var cleanSections = new List<ExpandoObject>();

            foreach (var section in sections)
            {
                dynamic expSection = new ExpandoObject();
                var sectionDict = (IDictionary<string, object>)expSection;
                sectionDict["sectionId"] = section.sectionId;
                sectionDict["sectionTitle"] = section.sectionTitle ?? "";

                var details = (section.details as IEnumerable<dynamic>)?.ToList() ?? new List<dynamic>();
                sectionDict["details"] = details.Select(d => {
                    dynamic expDetail = new ExpandoObject();
                    ((IDictionary<string, object>)expDetail)["content"] = d.content ?? "";
                    return expDetail;
                }).ToList();

                cleanSections.Add(expSection);
            }

            benefitDict["sections"] = cleanSections;
            var tags = (benefit.tags as IEnumerable<string>)?.ToList() ?? new List<string>();
            benefitDict["tags"] = tags;

            // Keywords
            var keywords = new List<string>();
            if (!string.IsNullOrEmpty((string)benefit.title)) keywords.Add(benefit.title.ToLower());
            keywords.AddRange(tags.Select(t => t.ToLower()));
            benefitDict["keywords"] = keywords;

            // Search Text
            var allSectionTitles = string.Join(" ", cleanSections.Select(s => (string)((IDictionary<string, object>)s)["sectionTitle"]));
            var allContents = string.Join(" ", cleanSections.SelectMany(s => 
                ((IEnumerable<dynamic>)((IDictionary<string, object>)s)["details"]).Select(d => (string)((IDictionary<string, object>)d)["content"])));

            benefitDict["search_text"] = $"{benefit.title} {benefit.description} {allSectionTitles} {allContents} {string.Join(" ", tags)}";

            return expBenefit;
        }
    }
}

