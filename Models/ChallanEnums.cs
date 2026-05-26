namespace CampusSentinel.Models
{
    /// <summary>
    /// Represents the lifecycle status of a campus challan (fine/penalty).
    /// Stored as an integer discriminator in the database.
    /// </summary>
    public enum ChallanStatus
    {
        /// <summary>The challan has been issued but not yet settled.</summary>
        Pending = 0,

        /// <summary>The fine amount has been paid in full.</summary>
        Paid = 1,

        /// <summary>The subject has formally contested the challan.</summary>
        Disputed = 2,

        /// <summary>The challan was voided by an administrator.</summary>
        Cancelled = 3
    }
}
