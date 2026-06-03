using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusSentinel.Models;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CampusSentinel.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;
        private readonly INotificationService _notificationService;

        public IncidentService(ApplicationDbContext context, IPdfService pdfService, INotificationService notificationService)
        {
            _context = context;
            _pdfService = pdfService;
            _notificationService = notificationService;
        }

        public async Task<Incident> CreateIncidentAsync(Incident incident)
        {
            incident.ReportedAt = DateTime.Now;
            incident.Status = IncidentStatus.Open;
            
            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();

            // Trigger Notifications
            var admins = await _context.Admins.ToListAsync();
            var tokens = new Dictionary<string, string>
            {
                { "Title", incident.Title },
                { "Location", incident.Location },
                { "SeverityLevel", incident.Severity.ToString() },
                { "Time", incident.ReportedAt.ToString("HH:mm") }
            };

            foreach (var admin in admins)
            {
                await _notificationService.SendAsync(NotificationEventType.IncidentLogged, admin.Id, tokens, incident.Id, "Incident");
            }

            return incident;
        }

        public async Task<bool> UpdateStatusAsync(int incidentId, IncidentStatus newStatus, int updatedByUserId)
        {
            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident == null) return false;

            if (incident.Status == newStatus) return true;

            var oldStatus = incident.Status;
            incident.Status = newStatus;

            if (newStatus == IncidentStatus.Resolved)
            {
                incident.ResolvedAt = DateTime.Now;
            }

            var note = new IncidentNote
            {
                IncidentId = incidentId,
                Note = $"Status changed from {oldStatus} to {newStatus}",
                AddedById = updatedByUserId,
                AddedAt = DateTime.Now
            };

            _context.IncidentNotes.Add(note);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IncidentNote> AddNoteAsync(int incidentId, string noteText, int userId)
        {
            var note = new IncidentNote
            {
                IncidentId = incidentId,
                Note = noteText,
                AddedById = userId,
                AddedAt = DateTime.Now
            };

            _context.IncidentNotes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<(List<Incident> Items, int TotalCount)> GetPagedIncidentsAsync(
            int? reportedByUserId = null,
            IncidentSeverity? severity = null,
            IncidentStatus? status = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Incidents
                .Include(i => i.ReportedBy)
                .AsQueryable();

            if (reportedByUserId.HasValue)
                query = query.Where(i => i.ReportedById == reportedByUserId.Value);

            if (severity.HasValue)
                query = query.Where(i => i.Severity == severity.Value);

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.ReportedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Incident> GetIncidentByIdAsync(int id)
        {
            return await _context.Incidents
                .Include(i => i.ReportedBy)
                .Include(i => i.LinkedAccessLog)
                .Include(i => i.Notes)
                    .ThenInclude(n => n.AddedBy)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Incident> UpdateIncidentAsync(Incident incident, int updatedByUserId)
        {
            var existing = await _context.Incidents.FindAsync(incident.Id);
            if (existing != null)
            {
                existing.Title = incident.Title;
                existing.Description = incident.Description;
                existing.Severity = incident.Severity;
                existing.Location = incident.Location;
                
                await _context.SaveChangesAsync();
                
                // If status changed, handle it via UpdateStatusAsync to ensure notes are logged
                if (existing.Status != incident.Status)
                {
                    await UpdateStatusAsync(existing.Id, incident.Status, updatedByUserId);
                }
            }
            return existing;
        }

        public async Task<byte[]> GenerateIncidentPdfAsync(int incidentId)
        {
            var incident = await GetIncidentByIdAsync(incidentId);
            if (incident == null) throw new Exception("Incident not found");

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);

                    void ComposeHeader(IContainer headerContainer)
                    {
                        headerContainer.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("CAMPUS SENTINEL").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text("Official Incident Report").FontSize(14).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(100).AlignRight().Text($"#{incident.Id:D5}").FontSize(16).Bold();
                        });
                    }

                    void ComposeContent(IContainer contentContainer)
                    {
                        contentContainer.PaddingVertical(1, Unit.Centimetre).Column(column =>
                        {
                            column.Spacing(20);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Title").SemiBold();
                                    col.Item().Text(incident.Title);
                                    
                                    col.Item().PaddingTop(10).Text("Reported By").SemiBold();
                                    col.Item().Text(incident.ReportedBy?.Username ?? "Unknown");
                                });
                                
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Date Reported").SemiBold();
                                    col.Item().Text(incident.ReportedAt.ToString("HH:mm"));
                                    
                                    col.Item().PaddingTop(10).Text("Status").SemiBold();
                                    col.Item().Text(incident.Status.ToString()).FontColor(incident.Status == IncidentStatus.Open ? Colors.Red.Medium : Colors.Green.Medium);
                                });
                                
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Severity").SemiBold();
                                    col.Item().Text(incident.Severity.ToString()).FontColor(incident.Severity == IncidentSeverity.Critical ? Colors.Red.Darken2 : Colors.Orange.Medium);
                                    
                                    col.Item().PaddingTop(10).Text("Location").SemiBold();
                                    col.Item().Text(incident.Location);
                                });
                            });

                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            column.Item().Column(col =>
                            {
                                col.Item().PaddingBottom(5).Text("Description").SemiBold().FontSize(12);
                                col.Item().Text(incident.Description);
                            });

                            if (incident.Notes.Any())
                            {
                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                column.Item().PaddingBottom(5).Text("Activity Log & Notes").SemiBold().FontSize(12);
                                
                                foreach (var note in incident.Notes.OrderBy(n => n.AddedAt))
                                {
                                    column.Item().PaddingBottom(10).Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
                                    {
                                        col.Item().Text($"{note.AddedBy?.Username ?? "Unknown"} - {note.AddedAt:HH:mm}").SemiBold().FontSize(10).FontColor(Colors.Grey.Darken1);
                                        col.Item().Text(note.Note);
                                    });
                                }
                            }
                        });
                    }

                    void ComposeFooter(IContainer footerContainer)
                    {
                        footerContainer.AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    }
                });
            });

            return document.GeneratePdf();
        }
    }
}
