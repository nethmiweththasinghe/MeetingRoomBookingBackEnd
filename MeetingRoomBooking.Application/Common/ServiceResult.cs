namespace MeetingRoomBooking.Application.Common
{
    public enum ResultStatus
    {
        Ok,
        NotFound,   // -> 404
        Invalid,    // -> 400 (broke a rule)
        Conflict    // -> 409 (clashes with existing data)
    }

    /// <summary>
    /// What a service method returns: either a value, or the reason it failed.
    /// The controller turns this into the right HTTP status code.
    /// </summary>
    public record ServiceResult<T>(ResultStatus Status, T? Value = default, string? Error = null)
    {
        public static ServiceResult<T> Ok(T value) => new(ResultStatus.Ok, value);
        public static ServiceResult<T> NotFound(string error) => new(ResultStatus.NotFound, default, error);
        public static ServiceResult<T> Invalid(string error) => new(ResultStatus.Invalid, default, error);
        public static ServiceResult<T> Conflict(string error) => new(ResultStatus.Conflict, default, error);
    }
}