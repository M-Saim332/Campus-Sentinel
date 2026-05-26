namespace CampusSentinel.Models
{
    public enum NotificationEventType
    {
        BlacklistAlert,
        CapacityThreshold,
        AfterHoursAccess,
        IncidentLogged,
        ShiftMissed,
        UnauthorizedScan
    }

    public enum NotificationChannel
    {
        InApp,
        Email,
        SMS
    }

    public enum NotificationStatus
    {
        Sent,
        Failed,
        Pending
    }
}
