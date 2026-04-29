

using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KinectCare.API.Services;

public class PdfService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public PdfService(
        AppDbContext db,
        IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ApiResponse<string>>
        GenerateReportPdfAsync(int reportId)
    {
        var report = await _db.Reports
            .Include(r => r.Child)
            .Include(r => r.Specialist)
            .Include(r => r.Session)
                .ThenInclude(s => s!.AIAnalysisResult)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return ApiResponse<string>.Fail("التقرير غير موجود");

        var analysis = report.Session?.AIAnalysisResult;

        var overall = analysis != null
            ? Math.Round(
                (analysis.PoseScore +
                 analysis.HandScore +
                 analysis.ActivityLevel +
                 analysis.AttentionScore) / 4.0, 1)
            : 0.0;

        // مسار الحفظ
        var folder = Path.Combine(_env.WebRootPath, "reports");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName =
            $"report_{reportId}_{DateTime.Now:yyyyMMddHHmm}.pdf";
        var filePath = Path.Combine(folder, fileName);

        // بناء الـ PDF
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x =>
                    x.FontSize(11));

                page.Header()
                    .Element(c => ComposeHeader(c));

                page.Content()
                    .Element(c => ComposeContent(
                        c, report, analysis, overall));

                page.Footer()
                    .Element(c => ComposeFooter(c));
            });
        }).GeneratePdf(filePath);

        // حفظ المسار في DB
        report.ReportFilePath = $"/reports/{fileName}";
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok(
            $"/reports/{fileName}",
            "تم إنشاء ملف PDF بنجاح");
    }

    // ─────────────────────────────────────────────────────
    // Header
    // ─────────────────────────────────────────────────────
    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item()
                     .Text("KinectCare")
                     .FontSize(20)
                     .Bold()
                     .FontColor("#1D9E75");

                    c.Item()
                     .Text("نظام متابعة الأطفال ذوي الاحتياجات الخاصة")
                     .FontSize(9)
                     .FontColor("#888888");
                });

                row.ConstantItem(120)
                   .AlignRight()
                   .Column(c =>
                   {
                       c.Item()
                        .Text("تقرير الجلسة")
                        .FontSize(14)
                        .Bold();

                       c.Item()
                        .Text(DateTime.Now.ToString("dd/MM/yyyy"))
                        .FontSize(9)
                        .FontColor("#888888");
                   });
            });

            // خط فاصل
            col.Item()
               .PaddingTop(8)
               .LineHorizontal(1)
               .LineColor("#1D9E75");
        });
    }

    // ─────────────────────────────────────────────────────
    // Content — ✅ استبدلنا dynamic بالأنواع الحقيقية
    // ─────────────────────────────────────────────────────
    private void ComposeContent(
        IContainer container,
        Report report,           // ← النوع الحقيقي
        AIAnalysisResult? analysis,  // ← النوع الحقيقي
        double overall)
    {
        container.PaddingTop(16).Column(col =>
        {
            // ── بيانات الطفل والأخصائي ──
            col.Item().Row(row =>
            {
                // بيانات الطفل
                row.RelativeItem()
                   .Border(1)
                   .BorderColor("#E5E7EB")
                   .Padding(12)
                   .Column(c =>
                   {
                       c.Item()
                        .Text("بيانات الطفل")
                        .Bold()
                        .FontColor("#1D9E75")
                        .FontSize(11);

                       c.Item()
                        .PaddingTop(6)
                        .Text($"الاسم: {report.Child.FullName}");

                       c.Item()
                        .Text($"التشخيص: {report.Child.DiagnosisType}");
                   });

                row.ConstantItem(12);

                // بيانات الأخصائي
                row.RelativeItem()
                   .Border(1)
                   .BorderColor("#E5E7EB")
                   .Padding(12)
                   .Column(c =>
                   {
                       c.Item()
                        .Text("بيانات الأخصائي")
                        .Bold()
                        .FontColor("#1D9E75")
                        .FontSize(11);

                       c.Item()
                        .PaddingTop(6)
                        .Text($"الاسم: {report.Specialist.FullName}");

                       c.Item()
                        .Text($"نوع الجلسة: " +
                              $"{report.Session?.ExerciseType ?? "—"}");

                       c.Item()
                        .Text($"تاريخ الجلسة: " +
                              $"{report.Session?.SessionDate.ToString("dd/MM/yyyy") ?? "—"}");
                   });
            });

            col.Item().PaddingTop(16);

            // ── نتائج AI ──
            if (analysis != null)
            {
                col.Item()
                   .Background("#F0FDF4")
                   .Border(1)
                   .BorderColor("#BBF7D0")
                   .Padding(12)
                   .Column(c =>
                   {
                       c.Item()
                        .Text("نتائج تحليل الذكاء الاصطناعي")
                        .Bold()
                        .FontColor("#1D9E75")
                        .FontSize(12);

                       c.Item().PaddingTop(8).Row(r =>
                       {
                           ScoreItem(r, "وضعية الجسم",
                               analysis.PoseScore);
                           ScoreItem(r, "حركة اليدين",
                               analysis.HandScore);
                           ScoreItem(r, "مستوى النشاط",
                               analysis.ActivityLevel);
                           ScoreItem(r, "الانتباه",
                               analysis.AttentionScore);
                           ScoreItem(r, "الأداء العام",
                               overall);
                       });

                       if (!string.IsNullOrEmpty(
                               analysis.OverallSummary))
                       {
                           c.Item()
                            .PaddingTop(8)
                            .Text(analysis.OverallSummary)
                            .FontSize(10)
                            .FontColor("#374151");
                       }
                   });

                col.Item().PaddingTop(12);
            }

            // ── أقسام التقرير النصية ──
            ReportSection(col,
                "ملاحظات الجلسة",
                report.SessionObservations);

            ReportSection(col,
                "تقييم التقدم",
                report.ProgressAssessment);

            ReportSection(col,
                "التحديات الملحوظة",
                report.ChallengesNoticed);

            ReportSection(col,
                "توصيات لأولياء الأمور",
                report.RecommendationsForParents,
                "#FFFBEB",
                "#FEF3C7");
        });
    }

    // ─────────────────────────────────────────────────────
    // Score Item
    // ─────────────────────────────────────────────────────
    private void ScoreItem(
        RowDescriptor row,
        string label,
        double value)
    {
        row.RelativeItem().AlignCenter().Column(c =>
        {
            c.Item()
             .AlignCenter()
             .Text($"{Math.Round(value, 1)}%")
             .Bold()
             .FontSize(16)
             .FontColor("#1D9E75");

            c.Item()
             .AlignCenter()
             .Text(label)
             .FontSize(9)
             .FontColor("#6B7280");
        });
    }

    // ─────────────────────────────────────────────────────
    // Report Section
    // ─────────────────────────────────────────────────────
    private void ReportSection(
        ColumnDescriptor col,
        string title,
        string? content,
        string bg = "#F9FAFB",
        string border = "#E5E7EB")
    {
        if (string.IsNullOrEmpty(content)) return;

        col.Item()
           .Background(bg)
           .Border(1)
           .BorderColor(border)
           .Padding(12)
           .Column(c =>
           {
               c.Item()
                .Text(title)
                .Bold()
                .FontSize(11)
                .FontColor("#374151");

               c.Item()
                .PaddingTop(6)
                .Text(content)
                .FontSize(10)
                .FontColor("#4B5563")
                .LineHeight(1.5f);
           });

        col.Item().PaddingTop(8);
    }

    // ─────────────────────────────────────────────────────
    // Footer
    // ─────────────────────────────────────────────────────
    private void ComposeFooter(IContainer container)
    {
        container
            .BorderTop(1)
            .BorderColor("#E5E7EB")
            .PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem()
                   .Text("KinectCare — نظام متابعة الأطفال")
                   .FontSize(8)
                   .FontColor("#9CA3AF");

                row.ConstantItem(100)
                   .AlignRight()
                   .Text(x =>
                   {
                       x.Span("صفحة ")
                        .FontSize(8)
                        .FontColor("#9CA3AF");

                       x.CurrentPageNumber()
                        .FontSize(8)
                        .FontColor("#9CA3AF");
                   });
            });
    }
}