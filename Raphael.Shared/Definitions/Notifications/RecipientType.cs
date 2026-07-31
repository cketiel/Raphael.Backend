namespace Raphael.Shared.Definitions.Notifications;

public sealed class RecipientType : NotificationEnumeration
{
    public static readonly RecipientType Driver =
        new(
            1,
            "DRIVER",
            "Driver",
            "A driver using the Raphael.Driver mobile application.",
            1,
            true,
            false);

    public static readonly RecipientType Rider =
        new(
            2,
            "RIDER",
            "Rider",
            "A patient or member using the Raphael.Rider application.",
            2,
            true,
            false);

    public static readonly RecipientType DesktopUser =
        new(
            3,
            "DESKTOP_USER",
            "Desktop User",
            "An office user working with Raphael.Desktop.",
            3,
            true,
            false);

    public static readonly RecipientType Integration =
        new(
            4,
            "INTEGRATION",
            "Integration",
            "An external system integrated with Raphael.",
            4,
            false,
            true);

    public static readonly RecipientType System =
        new(
            5,
            "SYSTEM",
            "System",
            "Internal Raphael services and background processes.",
            5,
            false,
            true);

    /// <summary>
    /// Indicates whether the recipient represents a human user.
    /// </summary>
    public bool IsHuman { get; }

    /// <summary>
    /// Indicates whether the recipient represents a system or external integration.
    /// </summary>
    public bool IsSystem { get; }

    private RecipientType(
        int id,
        string code,
        string name,
        string description,
        int sortOrder,
        bool isHuman,
        bool isSystem)
        : base(
            id,
            code,
            name,
            description,
            sortOrder)
    {
        IsHuman = isHuman;
        IsSystem = isSystem;
    }
}