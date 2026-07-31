
namespace Raphael.Notification.Domain.Definitions;

public sealed class NotificationSeverity : NotificationEnumeration
{
    public static readonly NotificationSeverity Information =
        new(
            1,
            "INFO",
            "Information",
            "Information",
            "Informational notification.",
            1,
            false,
            false);

    public static readonly NotificationSeverity Success =
        new(
            2,
            "SUCCESS",
            "Success",
            "Success",
            "Successful operation notification.",
            2,
            false,
            false);

    public static readonly NotificationSeverity Warning =
        new(
            3,
            "WARN",
            "Warning",
            "Warning",
            "Warning notification that requires attention.",
            3,
            true,
            false);

    public static readonly NotificationSeverity Error =
        new(
            4,
            "ERROR",
            "Error",
            "Error",
            "Error notification.",
            4,
            true,
            false);

    public static readonly NotificationSeverity Critical =
        new(
            5,
            "CRITICAL",
            "Critical",
            "Critical",
            "Critical notification requiring immediate action.",
            5,
            true,
            true);

    /*public string DisplayName { get; }

    public string Description { get; }

    public int SortOrder { get; }*/

    public bool RequiresAttention { get; }

    /*RequiresImmediateAction

    Será muy útil para reglas como:

    Mostrar un diálogo modal.
    Reproducir un sonido.
    Enviar Push Notification inmediatamente.
    Evitar que la alerta quede oculta en el Notification Center.*/
    public bool RequiresImmediateAction { get; }

    public bool IsCritical =>
        this == Critical;

    private NotificationSeverity(
        int id,
        string code,
        string name,
        string displayName,
        string description,
        int sortOrder,
        bool requiresAttention,
        bool requiresImmediateAction)
        : base(id, code, name, displayName, description, sortOrder)
    {      
        RequiresAttention = requiresAttention;
        RequiresImmediateAction = requiresImmediateAction;
    }
}