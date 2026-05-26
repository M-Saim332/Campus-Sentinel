using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    public interface ISchedulerService
    {
        Task<List<GuardShift>> GetWeeklyScheduleAsync(DateOnly weekStartDate);
        Task<List<GuardShift>> GetGuardScheduleAsync(int guardId, DateOnly weekStartDate);
        Task<GuardShift> AssignShiftAsync(int guardUserId, int zoneId, DateOnly date, TimeOnly start, TimeOnly end, int adminId);
        Task<bool> DetectConflictsAsync(int guardUserId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeShiftId = null);
        Task<bool> CompleteShiftAsync(int shiftId);
        Task<ShiftSwapRequest> RequestSwapAsync(int requestingGuardId, int targetGuardId, int shiftId, string reason);
        Task<bool> ResolveSwapAsync(int swapRequestId, bool approved, int adminId);
        Task<byte[]> GenerateWeeklyRosterPdfAsync(DateOnly weekStartDate);
        Task<List<User>> GetActiveGuardsNowAsync();
        Task<List<ShiftSwapRequest>> GetPendingSwapRequestsAsync();
    }
}
