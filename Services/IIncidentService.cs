using System.Collections.Generic;
using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    public interface IIncidentService
    {
        Task<Incident> CreateIncidentAsync(Incident incident);
        Task<bool> UpdateStatusAsync(int incidentId, IncidentStatus newStatus, int updatedByUserId);
        Task<IncidentNote> AddNoteAsync(int incidentId, string noteText, int userId);
        Task<(List<Incident> Items, int TotalCount)> GetPagedIncidentsAsync(
            int? reportedByUserId = null,
            IncidentSeverity? severity = null,
            IncidentStatus? status = null,
            int page = 1,
            int pageSize = 10);
        Task<Incident> GetIncidentByIdAsync(int id);
        Task<Incident> UpdateIncidentAsync(Incident incident, int updatedByUserId);
        Task<byte[]> GenerateIncidentPdfAsync(int incidentId);
    }
}
