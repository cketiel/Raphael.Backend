using System.Collections.Concurrent;
using System.Reflection;
using Raphael.Shared.Domain.Exceptions;

namespace Raphael.Shared.Domain.Common;

public abstract class Enumeration :
    IComparable,
    IEquatable<Enumeration>
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<Enumeration>> Cache = new();

    public int Id { get; }

    public string Code { get; }

    public string Name { get; }

    protected Enumeration(
        int id,
        string code,
        string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Code = code;
        Name = name;
    }

    public override string ToString()
        => Name;

    public bool Equals(Enumeration? other)
    {
        if (other is null)
            return false;

        return GetType() == other.GetType()
               && Id == other.Id;
    }

    public override bool Equals(object? obj)
        => Equals(obj as Enumeration);

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public int CompareTo(object? obj)
    {
        if (obj is not Enumeration other)
            throw new ArgumentException(
                $"Object must be of type {nameof(Enumeration)}.");

        return Id.CompareTo(other.Id);
    }

    public static IReadOnlyCollection<T> GetAll<T>()
        where T : Enumeration
    {
        var type = typeof(T);

        if (Cache.TryGetValue(type, out var cached))
            return cached.Cast<T>().ToList().AsReadOnly();

        var items = type
            .GetFields(BindingFlags.Public |
                       BindingFlags.Static |
                       BindingFlags.DeclaredOnly)
            .Select(field => field.GetValue(null))
            .OfType<T>()
            .OrderBy(e => e.Id)
            .ToList()
            .AsReadOnly();

        Cache[type] = items.Cast<Enumeration>().ToList().AsReadOnly();

        return items;
    }

    public static T FromId<T>(int id)
        where T : Enumeration
    {
        return TryFromId<T>(id, out var value)
            ? value!
            : throw new InvalidEnumerationException(typeof(T), id.ToString());
    }

    public static T FromCode<T>(string code)
        where T : Enumeration
    {
        return TryFromCode<T>(code, out var value)
            ? value!
            : throw new InvalidEnumerationException(typeof(T), code);
    }

    public static bool TryFromId<T>(
        int id,
        out T? value)
        where T : Enumeration
    {
        value = GetAll<T>()
            .FirstOrDefault(e => e.Id == id);

        return value is not null;
    }

    public static bool TryFromCode<T>(
        string code,
        out T? value)
        where T : Enumeration
    {
        value = GetAll<T>()
            .FirstOrDefault(e =>
                string.Equals(
                    e.Code,
                    code,
                    StringComparison.OrdinalIgnoreCase));

        return value is not null;
    }
}