using System.Collections.Generic;
using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    /// <summary>
    /// Defines the contract for the Challan Generation Service.
    ///
    /// Responsibilities:
    ///   1. Decoding and validating a QR code payload to resolve a campus person.
    ///   2. Issuing (persisting) a validated challan against that person.
    ///   3. Querying challans for display in the admin dashboard.
    ///   4. Updating challan status (Paid / Disputed / Cancelled).
    /// </summary>
    public interface IChallanService
    {
        /// <summary>
        /// Validates the format of a raw QR code string, then performs an efficient
        /// multi-table lookup to resolve the person it belongs to.
        /// </summary>
        /// <param name="qrCode">The decoded QR code string from the scanner.</param>
        /// <returns>
        ///   A <see cref="ChallanSubjectDto"/> on success, or <c>null</c> if the
        ///   code does not match any known Student, Staff, or Visitor.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        ///   Thrown when the QR code string fails format / security validation.
        /// </exception>
        Task<ChallanSubjectDto?> ResolveQrCodeAsync(string qrCode);

        /// <summary>
        /// Validates the <paramref name="request"/> payload and persists a new challan
        /// to the database.
        /// </summary>
        /// <returns>
        ///   A value tuple: <c>Success</c> flag, generated <c>ChallanId</c>,
        ///   and a human-readable <c>Error</c> message on failure.
        /// </returns>
        Task<(bool Success, int ChallanId, string Error)> IssueChallanAsync(IssueChallanRequest request);

        /// <summary>
        /// Returns all challans ordered by IssueDate descending, with the issuing
        /// user navigation property populated.
        /// </summary>
        Task<IEnumerable<Challan>> GetAllChallansAsync();

        /// <summary>Returns a single challan by its primary key, or <c>null</c> if not found.</summary>
        Task<Challan?> GetChallanByIdAsync(int id);

        /// <summary>
        /// Changes the status of an existing challan and records optional notes.
        /// Only an Admin user may call this operation (enforced at the page layer).
        /// </summary>
        /// <returns><c>true</c> if the record was found and updated; <c>false</c> otherwise.</returns>
        Task<bool> UpdateStatusAsync(int challanId, ChallanStatus newStatus, string? notes);
    }
}
