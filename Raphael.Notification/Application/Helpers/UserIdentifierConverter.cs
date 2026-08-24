using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Helpers;

/// <summary>
/// Translates the integer identifiers used across Raphael (CustomerId, UserId,
/// IntegratorId, TripId) into the <see cref="Guid"/> the notification module stores.
/// </summary>
/// <remarks>
/// The integer is written into the first four bytes and the recipient type marker into
/// the last one. Without the marker, desktop user 5 and customer 5 produce the very same
/// Guid, and a notification meant for the dispatch office ends up on a patient's phone.
///
/// <para>
/// <see cref="RecipientType.Rider"/> is deliberately mapped to marker 0 so that rider
/// identifiers stay byte for byte identical to the ones already stored in production.
/// </para>
/// </remarks>
public static class UserIdentifierConverter
{
    /// <summary>
    /// Marker reserved for riders and for non-recipient identifiers such as a TripId
    /// used as an aggregate id. Keeps backwards compatibility with existing rows.
    /// </summary>
    private const byte LegacyMarker = 0;

    private const int MarkerIndex = 15;

    /// <summary>
    /// Identifier of the audience recipient that represents every Raphael.Desktop user.
    /// A single notification row is addressed to it instead of one row per dispatcher.
    /// </summary>
    public static readonly Guid DesktopAudience =
        ToGuid(0, RecipientType.DesktopUser);

    /// <summary>
    /// Legacy conversion, without a type marker. Used for riders and for aggregate
    /// identifiers (TripId), which are not recipients.
    /// </summary>
    public static Guid ToGuid(int userId)
    {
        return Build(userId, LegacyMarker);
    }

    /// <summary>
    /// Converts an identifier into the Guid of a recipient of the given type.
    /// </summary>
    public static Guid ToGuid(int id, RecipientType recipientType)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        return Build(id, MarkerFor(recipientType));
    }

    /// <summary>
    /// Recovers the integer identifier. The type marker does not take part.
    /// </summary>
    public static int ToInt(Guid userGuid)
    {
        return BitConverter.ToInt32(userGuid.ToByteArray(), 0);
    }

    /// <summary>
    /// True when the Guid addresses the whole Raphael.Desktop audience rather than
    /// one concrete dispatcher.
    /// </summary>
    public static bool IsDesktopAudience(Guid recipientId)
    {
        return recipientId == DesktopAudience;
    }

    private static byte MarkerFor(RecipientType recipientType)
    {
        // Rider keeps marker 0 so the identifiers already persisted remain valid.
        // No other recipient type can collide with it: enumeration ids start at 1.
        return recipientType.Id == RecipientType.Rider.Id
            ? LegacyMarker
            : (byte)recipientType.Id;
    }

    private static Guid Build(int id, byte marker)
    {
        Span<byte> bytes = stackalloc byte[16];

        BitConverter.GetBytes(id).CopyTo(bytes);

        bytes[MarkerIndex] = marker;

        return new Guid(bytes);
    }
}
