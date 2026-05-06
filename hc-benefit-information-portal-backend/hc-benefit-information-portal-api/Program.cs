using Microsoft.EntityFrameworkCore; // Tambahkan di paling atas
using hc_benefit_information_portal_api.Data; // Sesuaikan namespace folder Data Anda
using System;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using Meilisearch;
using hc_benefit_information_portal_api.Services;


var builder = WebApplication.CreateBuilder(args);

// =========================
// DATABASE CONNECTION (STEP 3)
// =========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSingleton(new MeilisearchClient("http://localhost:7700"));
// =========================
// CORS
// =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// =========================
// REGISTER SERVICES
// =========================
builder.Services.AddControllers();
builder.Services.AddScoped<BenefitFaqServices>();
builder.Services.AddScoped<BenefitService>();

var app = builder.Build();
app.UseCors("AllowVue");

// =========================
// INIT MEILISEARCH
// =========================
var client = new MeilisearchClient("http://localhost:7700");
var health = await client.HealthAsync();
Console.WriteLine($"Meili status: {health.Status}");

// =========================
// SYNC DATA KE MEILI
// =========================
using (var scope = app.Services.CreateScope())
{
    // ===== BENEFIT =====
    var benefitService = scope.ServiceProvider.GetRequiredService<BenefitService>();
    var rawBenefits = await benefitService.GetAllBenefits(null);

    if (rawBenefits != null)
    {
        var benefits = rawBenefits.Cast<dynamic>().ToList();
        var cleanedBenefits = new List<ExpandoObject>();

        foreach (dynamic benefit in benefits)
        {
            dynamic expBenefit = new ExpandoObject();
            var benefitDict = (IDictionary<string, object>)expBenefit;

            benefitDict["id"] = benefit.id ?? Guid.NewGuid().ToString();
            benefitDict["title"] = benefit.title ?? "";
            benefitDict["description"] = benefit.description ?? "";
            benefitDict["category"] = benefit.category ?? 0;

            // Sections
            var sections = (benefit.sections as IEnumerable<dynamic>)?.ToList() ?? new List<dynamic>();
            var cleanSections = new List<ExpandoObject>();

            foreach (var section in sections)
            {
                dynamic expSection = new ExpandoObject();
                var sectionDict = (IDictionary<string, object>)expSection;

                sectionDict["sectionId"] = section.sectionId ?? Guid.NewGuid().ToString();
                sectionDict["sectionTitle"] = section.sectionTitle ?? "";

                var details = (section.details as IEnumerable<dynamic>)?.ToList() ?? new List<dynamic>();
                var cleanDetails = details
                    .GroupBy(d => (string)(d.content ?? ""))
                    .Select(g =>
                    {
                        dynamic expDetail = new ExpandoObject();
                        ((IDictionary<string, object>)expDetail)["content"] = g.Key;
                        return expDetail;
                    }).ToList();

                sectionDict["details"] = cleanDetails;
                cleanSections.Add(expSection);
            }

            benefitDict["sections"] = cleanSections;

            // Tags & keywords
            var tags = (benefit.tags as IEnumerable<string>)?.ToList() ?? new List<string>();
            benefitDict["tags"] = tags;

            var keywords = new List<string>();
            if (!string.IsNullOrEmpty((string)benefit.title)) keywords.Add(((string)benefit.title).ToLower());
            keywords.AddRange(tags.Where(t => !string.IsNullOrEmpty(t)).Select(t => t.ToLower()));
            benefitDict["keywords"] = keywords;

            // Search text
            var allSectionTitles = string.Join(" ", cleanSections.Select(s => (string)((IDictionary<string, object>)s)["sectionTitle"]));
            var allContents = string.Join(" ", cleanSections
                .SelectMany(s => ((IEnumerable<dynamic>)((IDictionary<string, object>)s)["details"])
                    .Select(d => (string)((IDictionary<string, object>)d)["content"])));

            var allTags = string.Join(" ", tags);

            benefitDict["search_text"] = $"{benefit.title ?? ""} {benefit.description ?? ""} {allSectionTitles} {allContents} {allTags}";

            cleanedBenefits.Add(expBenefit);
        }

        // DELETE INDEX TERLEBIH DAHULU
        try { await client.DeleteIndexAsync("benefits"); } catch { }

        // CREATE INDEX MELALUI CLIENT
        await client.CreateIndexAsync("benefits", "id");
        var benefitIndex = client.Index("benefits");

        // SET SEARCHABLE ATTRIBUTES
        await benefitIndex.UpdateSearchableAttributesAsync(new[]
        {
            "keywords", "title", "tags", "description", "search_text"
        });

        await benefitIndex.UpdateFilterableAttributesAsync(new[] { "category" });
        
        await benefitIndex.AddDocumentsAsync(cleanedBenefits);
        Console.WriteLine($"Benefit synced: {cleanedBenefits.Count}");
    }

    // ===== FAQ =====
    var faqService = scope.ServiceProvider.GetRequiredService<BenefitFaqServices>();
    var rawFaqs = await faqService.GetAllFaq();
    if (rawFaqs != null)
    {
        var faqs = rawFaqs.Cast<dynamic>().ToList();

        try { await client.DeleteIndexAsync("faqs"); } catch { }

        // CREATE INDEX MELALUI CLIENT
        await client.CreateIndexAsync("faqs", "id");
        var faqIndex = client.Index("faqs");

        await faqIndex.UpdateFilterableAttributesAsync(new[] { "benefitId" });
        await faqIndex.AddDocumentsAsync(faqs);
        Console.WriteLine($"FAQ synced: {faqs.Count}");
    }
}

// =========================
// MAP CONTROLLERS
// =========================
app.MapControllers();
app.Run();