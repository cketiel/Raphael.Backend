namespace Raphael.Shared.Domain.Exceptions;

public sealed class InvalidEnumerationException : Exception
{
    public Type EnumerationType { get; }

    public string LookupValue { get; }

    public InvalidEnumerationException(
        Type enumerationType,
        string lookupValue)
        : base($"{enumerationType.Name} with value '{lookupValue}' was not found.")
    {
        EnumerationType = enumerationType;
        LookupValue = lookupValue;
    }
}