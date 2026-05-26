using CampusSentinel.Models;
using System.Collections.Generic;

namespace CampusSentinel.Services
{
    public interface IPdfService
    {
        byte[] GenerateVisitorIdCard(Visitor visitor);
        byte[] GenerateStudentIdCard(Student student);
        byte[] GenerateStaffIdCard(Staff staff);
        byte[] GenerateWeeklyReport(List<AccessLog> logs);
    }
}
