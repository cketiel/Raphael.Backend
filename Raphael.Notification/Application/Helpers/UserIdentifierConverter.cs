namespace Raphael.Notification.Application.Helpers;

public static class UserIdentifierConverter
{
    public static Guid ToGuid(int userId)
    {
        Span<byte> bytes = stackalloc byte[16];

        BitConverter.GetBytes(userId).CopyTo(bytes);

        return new Guid(bytes);
    }

    public static int ToInt(Guid userGuid)
    {
        return BitConverter.ToInt32(userGuid.ToByteArray(), 0);
    }
}