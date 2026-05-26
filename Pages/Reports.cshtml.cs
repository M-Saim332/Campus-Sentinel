using CampusSentinel.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CampusSentinel.Pages
{
    [Authorize(Policy = "RequireAdminRole")]
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ReportsModel(ApplicationDbContext db)
        {
            _db = db;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ── Filter range ──────────────────────────────────────────────
        public DateTime From { get; set; }
        public DateTime To   { get; set; }

        // ── Summary stats ─────────────────────────────────────────────
        public int TotalAccessLogs        { get; set; }
        public int TotalIncidents         { get; set; }
        public int TotalStudents          { get; set; }
        public int BlacklistedStudents    { get; set; }
        public int TotalStaff             { get; set; }
        public int TotalShifts            { get; set; }
        public int TotalVisitors          { get; set; }

        // ─────────────────────────────────────────────────────────────
        public async Task OnGetAsync(string? from, string? to)
        {
            From = DateTime.TryParse(from, out var f) ? f : DateTime.Today.AddMonths(-1);
            To   = DateTime.TryParse(to,   out var t) ? t : DateTime.Today;

            TotalAccessLogs     = await _db.AccessLogs.CountAsync(a => a.Timestamp >= From && a.Timestamp <= To.AddDays(1));
            TotalIncidents      = await _db.Incidents.CountAsync(i => i.ReportedAt >= From && i.ReportedAt <= To.AddDays(1));
            TotalStudents       = await _db.Students.CountAsync();
            BlacklistedStudents = await _db.Students.CountAsync(s => s.IsBlacklisted);
            TotalStaff          = await _db.Staff.CountAsync();
            TotalVisitors       = await _db.Visitors.CountAsync();

            var fromDate = DateOnly.FromDateTime(From);
            var toDate   = DateOnly.FromDateTime(To);
            TotalShifts = await _db.GuardShifts.CountAsync(s => s.ShiftDate >= fromDate && s.ShiftDate <= toDate);
        }

        // ─── Shared helpers ───────────────────────────────────────────
        private static void AddReportHeader(IContainer container, string title, string subtitle)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("CAMPUS SENTINEL").FontSize(18).Bold().FontColor("#00C9A7");
                        c.Item().Text("Security Management System").FontSize(9).FontColor("#8B95A8");
                    });
                    row.ConstantItem(140).AlignRight().Column(c =>
                    {
                        c.Item().Text(title).FontSize(11).Bold().FontColor("#F0F4FF");
                        c.Item().Text(subtitle).FontSize(8).FontColor("#8B95A8");
                        c.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor("#4A5568");
                    });
                });
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#00C9A7");
            });
        }

        private static void AddReportFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("Campus Sentinel – Confidential").FontSize(7).FontColor("#4A5568");
                row.ConstantItem(80).AlignRight().Text(ctx =>
                {
                    ctx.CurrentPageNumber().FontSize(7).FontColor("#4A5568");
                    ctx.Span(" / ").FontSize(7).FontColor("#4A5568");
                    ctx.TotalPages().FontSize(7).FontColor("#4A5568");
                });
            });
        }

        // Helper to render a header cell
        private static void HeaderCell(IContainer cell, string text)
        {
            cell.Background("#0F1829").Border(0.5f).BorderColor("#162033")
                .Padding(5).Text(text).Bold().FontSize(8).FontColor("#00C9A7");
        }

        // Helper to render a data cell
        private static void DataCell(IContainer cell, string text, int row,
            string? color = null)
        {
            var bg = row % 2 == 0 ? "#080E1A" : "#0F1829";
            var t = cell.Background(bg).Border(0.5f).BorderColor("#162033").Padding(4).Text(text);
            if (color != null) t.FontColor(color);
        }

        // ─── 1. Access Log PDF ────────────────────────────────────────
        public async Task<IActionResult> OnGetAccessLogPdfAsync(string from, string to)
        {
            var fromDate = DateTime.TryParse(from, out var f) ? f : DateTime.Today.AddMonths(-1);
            var toDate   = DateTime.TryParse(to,   out var t) ? t : DateTime.Today;

            var logs = await _db.AccessLogs
                .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate.AddDays(1))
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(s => s.FontSize(9).FontColor("#F0F4FF"));
                    page.Background().Background("#080E1A");

                    page.Header().Element(c => AddReportHeader(c, "Access Log Report", $"{fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}"));
                    page.Footer().Element(c => AddReportFooter(c));

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(32);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.4f);
                            cols.RelativeColumn(1.8f);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1.4f);
                            cols.RelativeColumn(2);
                        });

                        table.Header(hdr =>
                        {
                            foreach (var h in new[] { "#", "Timestamp", "Type", "Scanned ID", "Direction", "Status", "Gate", "Reason" })
                                HeaderCell(hdr.Cell(), h);
                        });

                        for (int i = 0; i < logs.Count; i++)
                        {
                            var l = logs[i];
                            var statusColor = l.Status == "Granted" ? "#22C55E" : "#EF4444";
                            DataCell(table.Cell(), $"{i + 1}", i, "#8B95A8");
                            DataCell(table.Cell(), l.Timestamp.ToString("dd MMM yy HH:mm"), i);
                            DataCell(table.Cell(), l.TargetType, i);
                            DataCell(table.Cell(), l.TargetId, i);
                            DataCell(table.Cell(), l.Direction, i);
                            DataCell(table.Cell(), l.Status, i, statusColor);
                            DataCell(table.Cell(), l.GateLocation ?? "—", i);
                            DataCell(table.Cell(), l.Reason ?? "—", i, "#8B95A8");
                        }

                        if (!logs.Any())
                            table.Cell().ColumnSpan(8).Padding(20).AlignCenter()
                                .Text("No records found for the selected period.").FontColor("#4A5568");
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf",
                $"AccessLog_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
        }

        // ─── 2. Incident PDF ──────────────────────────────────────────
        public async Task<IActionResult> OnGetIncidentPdfAsync(string from, string to)
        {
            var fromDate = DateTime.TryParse(from, out var f) ? f : DateTime.Today.AddMonths(-1);
            var toDate   = DateTime.TryParse(to,   out var t) ? t : DateTime.Today;

            var incidents = await _db.Incidents
                .Include(i => i.ReportedBy)
                .Where(i => i.ReportedAt >= fromDate && i.ReportedAt <= toDate.AddDays(1))
                .OrderByDescending(i => i.ReportedAt)
                .ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(s => s.FontSize(9).FontColor("#F0F4FF"));
                    page.Background().Background("#080E1A");

                    page.Header().Element(c => AddReportHeader(c, "Incident Report", $"{fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}"));
                    page.Footer().Element(c => AddReportFooter(c));

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1.2f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1.5f);
                        });

                        table.Header(hdr =>
                        {
                            foreach (var h in new[] { "#", "Title", "Location", "Severity", "Status", "Reported By", "Reported At" })
                                HeaderCell(hdr.Cell(), h);
                        });

                        for (int i = 0; i < incidents.Count; i++)
                        {
                            var inc = incidents[i];
                            var sevColor = inc.Severity.ToString() switch
                            {
                                "Critical" => "#EF4444",
                                "High"     => "#F59E0B",
                                "Medium"   => "#3B82F6",
                                _          => "#8B95A8"
                            };
                            var statusColor = inc.Status.ToString() switch
                            {
                                "Open"        => "#EF4444",
                                "Resolved"    => "#22C55E",
                                "UnderReview" => "#F59E0B",
                                _             => "#8B95A8"
                            };

                            DataCell(table.Cell(), $"{i + 1}", i, "#8B95A8");
                            DataCell(table.Cell(), inc.Title, i);
                            DataCell(table.Cell(), inc.Location ?? "—", i);
                            DataCell(table.Cell(), inc.Severity.ToString(), i, sevColor);
                            DataCell(table.Cell(), inc.Status.ToString(), i, statusColor);
                            DataCell(table.Cell(), inc.ReportedBy?.Username ?? "—", i);
                            DataCell(table.Cell(), inc.ReportedAt.ToString("dd MMM yy HH:mm"), i);
                        }

                        if (!incidents.Any())
                            table.Cell().ColumnSpan(7).Padding(20).AlignCenter()
                                .Text("No incidents found for the selected period.").FontColor("#4A5568");
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf",
                $"Incidents_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
        }

        // ─── 3. Student PDF ───────────────────────────────────────────
        public async Task<IActionResult> OnGetStudentPdfAsync()
        {
            var students = await _db.Students.OrderBy(s => s.FullName).ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(s => s.FontSize(9).FontColor("#F0F4FF"));
                    page.Background().Background("#080E1A");

                    page.Header().Element(c => AddReportHeader(c, "Student Directory", $"Total: {students.Count}"));
                    page.Footer().Element(c => AddReportFooter(c));

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);
                            cols.RelativeColumn(2.5f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(0.8f);
                            cols.RelativeColumn(0.8f);
                            cols.RelativeColumn(1);
                        });

                        table.Header(hdr =>
                        {
                            foreach (var h in new[] { "#", "Full Name", "Reg No.", "QR Code ID", "Dept", "Session", "Gender", "Status" })
                                HeaderCell(hdr.Cell(), h);
                        });

                        for (int i = 0; i < students.Count; i++)
                        {
                            var s = students[i];
                            var statusText  = s.IsBlacklisted ? "Blacklisted" : "Active";
                            var statusColor = s.IsBlacklisted ? "#EF4444" : "#22C55E";

                            DataCell(table.Cell(), $"{i + 1}", i, "#8B95A8");
                            DataCell(table.Cell(), s.FullName, i);
                            DataCell(table.Cell(), s.RegistrationNo, i);
                            DataCell(table.Cell(), s.QrCodeId ?? "—", i);
                            DataCell(table.Cell(), s.Department, i);
                            DataCell(table.Cell(), s.Session.ToString(), i);
                            DataCell(table.Cell(), s.Gender, i);
                            DataCell(table.Cell(), statusText, i, statusColor);
                        }
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf",
                $"StudentDirectory_{DateTime.Today:yyyyMMdd}.pdf");
        }

        // ─── 4. Staff PDF ─────────────────────────────────────────────
        public async Task<IActionResult> OnGetStaffPdfAsync()
        {
            var staff = await _db.Staff.OrderBy(s => s.FullName).ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(s => s.FontSize(9).FontColor("#F0F4FF"));
                    page.Background().Background("#080E1A");

                    page.Header().Element(c => AddReportHeader(c, "Staff Directory", $"Total: {staff.Count}"));
                    page.Footer().Element(c => AddReportFooter(c));

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);
                            cols.RelativeColumn(2.5f);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1);
                        });

                        table.Header(hdr =>
                        {
                            foreach (var h in new[] { "#", "Full Name", "Category", "Dept / University", "Designation", "Staff ID", "Status" })
                                HeaderCell(hdr.Cell(), h);
                        });

                        for (int i = 0; i < staff.Count; i++)
                        {
                            var s = staff[i];
                            var statusText  = s.IsBlacklisted ? "Blacklisted" : "Active";
                            var statusColor = s.IsBlacklisted ? "#EF4444" : "#22C55E";

                            DataCell(table.Cell(), $"{i + 1}", i, "#8B95A8");
                            DataCell(table.Cell(), s.FullName, i);
                            DataCell(table.Cell(), s.Category.ToString(), i);
                            DataCell(table.Cell(), s.DepartmentOrUni, i);
                            DataCell(table.Cell(), s.Designation, i);
                            DataCell(table.Cell(), s.StaffId, i);
                            DataCell(table.Cell(), statusText, i, statusColor);
                        }
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf",
                $"StaffDirectory_{DateTime.Today:yyyyMMdd}.pdf");
        }

        // ─── 5. Guard Shift PDF ───────────────────────────────────────
        public async Task<IActionResult> OnGetShiftPdfAsync(string from, string to)
        {
            var fromDate     = DateTime.TryParse(from, out var f) ? f : DateTime.Today.AddMonths(-1);
            var toDate       = DateTime.TryParse(to,   out var t) ? t : DateTime.Today;
            var fromDateOnly = DateOnly.FromDateTime(fromDate);
            var toDateOnly   = DateOnly.FromDateTime(toDate);

            var shifts = await _db.GuardShifts
                .Include(s => s.GuardUser)
                .Include(s => s.Zone)
                .Where(s => s.ShiftDate >= fromDateOnly && s.ShiftDate <= toDateOnly)
                .OrderBy(s => s.ShiftDate).ThenBy(s => s.StartTime)
                .ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(s => s.FontSize(9).FontColor("#F0F4FF"));
                    page.Background().Background("#080E1A");

                    page.Header().Element(c => AddReportHeader(c, "Guard Schedule Report", $"{fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}"));
                    page.Footer().Element(c => AddReportFooter(c));

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);
                            cols.RelativeColumn(1.6f);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1.2f);
                        });

                        table.Header(hdr =>
                        {
                            foreach (var h in new[] { "#", "Date", "Guard", "Zone", "Start", "End", "Status" })
                                HeaderCell(hdr.Cell(), h);
                        });

                        for (int i = 0; i < shifts.Count; i++)
                        {
                            var s = shifts[i];
                            var statusColor = s.Status.ToString() switch
                            {
                                "Active"    => "#22C55E",
                                "Completed" => "#8B95A8",
                                "Missed"    => "#EF4444",
                                "Swapped"   => "#F59E0B",
                                _           => "#3B82F6"
                            };

                            DataCell(table.Cell(), $"{i + 1}", i, "#8B95A8");
                            DataCell(table.Cell(), s.ShiftDate.ToString("dd MMM yyyy"), i);
                            DataCell(table.Cell(), s.GuardUser?.Username ?? "—", i);
                            DataCell(table.Cell(), s.Zone?.Name ?? "—", i);
                            DataCell(table.Cell(), s.StartTime.ToString("HH:mm"), i);
                            DataCell(table.Cell(), s.EndTime.ToString("HH:mm"), i);
                            DataCell(table.Cell(), s.Status.ToString(), i, statusColor);
                        }

                        if (!shifts.Any())
                            table.Cell().ColumnSpan(7).Padding(20).AlignCenter()
                                .Text("No shifts found for the selected period.").FontColor("#4A5568");
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf",
                $"GuardSchedule_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
        }

        // ─── 6. Visitor PDF ───────────────────────────────────────────
        public async Task<IActionResult> OnGetVisitorPdfAsync()
        {
            var visitors = await _db.Visitors.OrderByDescending(v => v.CreatedAt).ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(s => s.FontSize(9).FontColor("#F0F4FF"));
                    page.Background().Background("#080E1A");

                    page.Header().Element(c => AddReportHeader(c, "Visitor Log Report", $"Total: {visitors.Count}"));
                    page.Footer().Element(c => AddReportFooter(c));

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.2f);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.8f);
                            cols.RelativeColumn(1);
                        });

                        table.Header(hdr =>
                        {
                            foreach (var h in new[] { "#", "Visitor Name", "Role", "Purpose", "Registered", "Status" })
                                HeaderCell(hdr.Cell(), h);
                        });

                        for (int i = 0; i < visitors.Count; i++)
                        {
                            var v = visitors[i];
                            var statusText  = v.IsBlacklisted ? "Blacklisted" : "Active";
                            var statusColor = v.IsBlacklisted ? "#EF4444" : "#22C55E";

                            DataCell(table.Cell(), $"{i + 1}", i, "#8B95A8");
                            DataCell(table.Cell(), v.FullName, i);
                            DataCell(table.Cell(), v.Role, i);
                            DataCell(table.Cell(), v.Purpose ?? "—", i);
                            DataCell(table.Cell(), v.CreatedAt.ToString("dd MMM yy HH:mm"), i);
                            DataCell(table.Cell(), statusText, i, statusColor);
                        }
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf",
                $"VisitorLog_{DateTime.Today:yyyyMMdd}.pdf");
        }
    }
}
