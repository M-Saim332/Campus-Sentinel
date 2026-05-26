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
    public class SchedulerService : ISchedulerService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public SchedulerService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<GuardShift>> GetWeeklyScheduleAsync(DateOnly weekStartDate)
        {
            var weekEndDate = weekStartDate.AddDays(7);
            
            // Auto-transition to Missed logic
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var timeNow = TimeOnly.FromDateTime(now);
            
            var missedShiftsQuery = await _context.GuardShifts
                .Include(s => s.GuardUser)
                .Include(s => s.Zone)
                .Where(s => s.Status == ShiftStatus.Scheduled && 
                            (s.ShiftDate < today || (s.ShiftDate == today && s.StartTime.AddMinutes(30) < timeNow)))
                .ToListAsync();
                
            if (missedShiftsQuery.Any())
            {
                var admins = await _context.Admins.ToListAsync();
                foreach(var s in missedShiftsQuery)
                {
                    s.Status = ShiftStatus.Missed;
                    
                    // Trigger Notifications
                    var tokens = new Dictionary<string, string>
                    {
                        { "PersonName", s.GuardUser?.Username ?? "Unknown" },
                        { "Location", s.Zone?.Name ?? "Unknown" },
                        { "Time", s.StartTime.ToString("HH:mm") }
                    };

                    foreach (var admin in admins)
                    {
                        await _notificationService.SendAsync(NotificationEventType.ShiftMissed, admin.Id, tokens, s.Id, "GuardShift");
                    }
                }
                await _context.SaveChangesAsync();
            }

            return await _context.GuardShifts
                .Include(s => s.GuardUser)
                .Include(s => s.Zone)
                .Where(s => s.ShiftDate >= weekStartDate && s.ShiftDate < weekEndDate)
                .OrderBy(s => s.ShiftDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<GuardShift>> GetGuardScheduleAsync(int guardId, DateOnly weekStartDate)
        {
            var weekEndDate = weekStartDate.AddDays(7);
            return await _context.GuardShifts
                .Include(s => s.Zone)
                .Where(s => s.GuardUserId == guardId && s.ShiftDate >= weekStartDate && s.ShiftDate < weekEndDate)
                .OrderBy(s => s.ShiftDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<GuardShift> AssignShiftAsync(int guardUserId, int zoneId, DateOnly date, TimeOnly start, TimeOnly end, int adminId)
        {
            if (await DetectConflictsAsync(guardUserId, date, start, end))
            {
                throw new InvalidOperationException("Guard has a conflicting shift.");
            }

                var shift = new GuardShift
                {
                    GuardUserId = guardUserId,
                    ZoneId = zoneId,
                    ShiftDate = date,
                    StartTime = start,
                    EndTime = end,
                    Status = ShiftStatus.Scheduled,
                    Notes = "",
                    CreatedByAdminId = adminId
                };

            _context.GuardShifts.Add(shift);
            await _context.SaveChangesAsync();
            return shift;
        }

        public async Task<bool> DetectConflictsAsync(int guardUserId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeShiftId = null)
        {
            var query = _context.GuardShifts
                .Where(s => s.GuardUserId == guardUserId && s.ShiftDate == date && s.Status != ShiftStatus.Missed && s.Status != ShiftStatus.Swapped);

            if (excludeShiftId.HasValue)
            {
                query = query.Where(s => s.Id != excludeShiftId.Value);
            }

            var shifts = await query.ToListAsync();

            return shifts.Any(s => 
                (startTime >= s.StartTime && startTime < s.EndTime) ||
                (endTime > s.StartTime && endTime <= s.EndTime) ||
                (startTime <= s.StartTime && endTime >= s.EndTime)
            );
        }

        public async Task<bool> CompleteShiftAsync(int shiftId)
        {
            var shift = await _context.GuardShifts.FindAsync(shiftId);
            if (shift == null) return false;

            shift.Status = ShiftStatus.Completed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ShiftSwapRequest> RequestSwapAsync(int requestingGuardId, int targetGuardId, int shiftId, string reason)
        {
            var request = new ShiftSwapRequest
            {
                RequestingGuardId = requestingGuardId,
                TargetGuardId = targetGuardId,
                ShiftId = shiftId,
                Reason = reason,
                Status = SwapRequestStatus.Pending
            };

            _context.ShiftSwapRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<bool> ResolveSwapAsync(int swapRequestId, bool approved, int adminId)
        {
            var request = await _context.ShiftSwapRequests
                .Include(r => r.Shift)
                .FirstOrDefaultAsync(r => r.Id == swapRequestId);

            if (request == null || request.Status != SwapRequestStatus.Pending) return false;

            request.Status = approved ? SwapRequestStatus.Approved : SwapRequestStatus.Rejected;
            request.ResolvedAt = DateTime.Now;
            request.ResolvedByAdminId = adminId;

            if (approved)
            {
                if (await DetectConflictsAsync(request.TargetGuardId, request.Shift.ShiftDate, request.Shift.StartTime, request.Shift.EndTime))
                {
                    throw new InvalidOperationException("Target guard has a conflicting shift.");
                }

                request.Shift.GuardUserId = request.TargetGuardId;
                request.Shift.Status = ShiftStatus.Swapped;
                
                // Create new shift for target guard
                var newShift = new GuardShift
                {
                    GuardUserId = request.TargetGuardId,
                    ZoneId = request.Shift.ZoneId,
                    ShiftDate = request.Shift.ShiftDate,
                    StartTime = request.Shift.StartTime,
                    EndTime = request.Shift.EndTime,
                    Status = ShiftStatus.Scheduled,
                    CreatedByAdminId = adminId,
                    Notes = $"Swapped from {request.RequestingGuardId}"
                };
                _context.GuardShifts.Add(newShift);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetActiveGuardsNowAsync()
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var timeNow = TimeOnly.FromDateTime(now);

            return await _context.GuardShifts
                .Include(s => s.GuardUser)
                .Where(s => s.ShiftDate == today && s.StartTime <= timeNow && s.EndTime >= timeNow && (s.Status == ShiftStatus.Active || s.Status == ShiftStatus.Scheduled))
                .Select(s => s.GuardUser)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<ShiftSwapRequest>> GetPendingSwapRequestsAsync()
        {
            return await _context.ShiftSwapRequests
                .Include(r => r.RequestingGuard)
                .Include(r => r.TargetGuard)
                .Include(r => r.Shift)
                .Where(r => r.Status == SwapRequestStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<byte[]> GenerateWeeklyRosterPdfAsync(DateOnly weekStartDate)
        {
            var shifts = await GetWeeklyScheduleAsync(weekStartDate);
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header => 
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("CAMPUS SENTINEL").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"Weekly Guard Roster: {weekStartDate:MMM dd} - {weekStartDate.AddDays(6):MMM dd}").FontSize(14);
                            });
                        });
                    });

                    page.Content().Element(content => 
                    {
                        content.PaddingVertical(1, Unit.Centimetre).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Guard
                                columns.RelativeColumn(); // Zone
                                columns.RelativeColumn(); // Date
                                columns.RelativeColumn(); // Time
                                columns.RelativeColumn(); // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Guard").SemiBold();
                                header.Cell().Text("Zone").SemiBold();
                                header.Cell().Text("Date").SemiBold();
                                header.Cell().Text("Time").SemiBold();
                                header.Cell().Text("Status").SemiBold();
                            });

                            foreach (var shift in shifts)
                            {
                                table.Cell().Text(shift.GuardUser?.Username);
                                table.Cell().Text(shift.Zone?.Name);
                                table.Cell().Text(shift.ShiftDate.ToString("MMM dd"));
                                table.Cell().Text($"{shift.StartTime:HH:mm} - {shift.EndTime:HH:mm}");
                                table.Cell().Text(shift.Status.ToString());
                            }
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
