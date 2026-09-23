namespace Raphael.Api.Services.CallRequests
{
    public enum CallRequestOutcome
    {
        Ok,
        NotFound,
        Conflict,
        Invalid
    }

    /// <summary>
    /// Outcome of a change to a call request. On a conflict <see cref="Value"/> is how the request
    /// stands now, so the screen can show who got there first.
    /// </summary>
    public sealed class CallRequestResult<T>
    {
        public CallRequestOutcome Outcome { get; private init; }

        public T? Value { get; private init; }

        public string? Message { get; private init; }

        public static CallRequestResult<T> Ok(T value) =>
            new() { Outcome = CallRequestOutcome.Ok, Value = value };

        public static CallRequestResult<T> NotFound() =>
            new() { Outcome = CallRequestOutcome.NotFound };

        public static CallRequestResult<T> Conflict(T? current, string message) =>
            new() { Outcome = CallRequestOutcome.Conflict, Value = current, Message = message };

        public static CallRequestResult<T> Invalid(string message) =>
            new() { Outcome = CallRequestOutcome.Invalid, Message = message };
    }

    /// <summary>The dispatcher acting, with the name that goes on the timeline.</summary>
    public sealed record OfficeCaller(int UserId, string Name, int? ProviderId);
}
