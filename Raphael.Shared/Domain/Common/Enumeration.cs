using System.Reflection;

namespace Raphael.Shared.Domain.Common;

public abstract class Enumeration : IComparable
{
    public int Id { get; }

    public string Code { get; }

    public string Name { get; }

    protected Enumeration(int id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    public override string ToString()
        => Name;

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration other)
            return false;

        return GetType() == other.GetType()
            && Id == other.Id;
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public int CompareTo(object? other)
    {
        if (other is not Enumeration enumeration)
            throw new ArgumentException("Object is not an Enumeration.");

        return Id.CompareTo(enumeration.Id);
    }

    public static IEnumerable<T> GetAll<T>()
        where T : Enumeration
    {
        return typeof(T)
            .GetFields(BindingFlags.Public |
                       BindingFlags.Static |
                       BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
    }

    public static T FromId<T>(int id)
        where T : Enumeration
    {
        return GetAll<T>().Single(x => x.Id == id);
    }

    public static T FromCode<T>(string code)
        where T : Enumeration
    {
        return GetAll<T>()
            .Single(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}