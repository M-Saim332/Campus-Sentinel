namespace CampusSentinel.Models
{
    public enum IncidentSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum IncidentStatus
    {
        Open,
        UnderReview,
        Resolved,
        Closed
    }

    public enum PersonType
    {
        Student,
        Staff,
        Visitor,
        Unknown
    }
}
