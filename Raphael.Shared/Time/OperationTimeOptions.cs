namespace Raphael.Shared.Time;

/// <summary>
/// Where the business keeps its clock.
/// </summary>
/// <remarks>
/// ⚠️ The timezone of the machine running the API is never one of these values, and that is
/// the whole point. Hosting is an accident; the hour a patient is picked up is not.
/// </remarks>
public sealed class OperationTimeOptions
{
    public const string SectionName = "Operations";

    /// <summary>
    /// Fallback for a provider that has not declared a timezone yet.
    /// </summary>
    /// <remarks>
    /// IANA identifier — <c>America/New_York</c>. .NET 8 accepts these on Windows as well as
    /// Linux, so the same configuration file survives a move between them.
    ///
    /// <para>
    /// ⚠️ The API refuses to start if this is not a timezone the host recognises. Falling
    /// back to the machine's own zone is precisely the defect this whole mechanism exists to
    /// remove: it works until somebody moves the server, and then every trip quietly shifts
    /// by hours with nothing in the logs to say why.
    /// </para>
    /// </remarks>
    public string DefaultTimeZone { get; set; } = "America/New_York";

    /// <summary>
    /// The provider that stands for the broker's own operation.
    /// </summary>
    /// <remarks>
    /// A trip with no <c>ProviderId</c> is one the broker runs itself, so it takes this
    /// provider's timezone. Configurable rather than hardcoded because "the broker is row 1"
    /// is true of this deployment, not of the model.
    /// </remarks>
    public int BrokerProviderId { get; set; } = 1;
}
