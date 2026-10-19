namespace GamingPlatform.Service
{
    public enum ResultStatus { Ok, NotFound, Forbidden, Invalid }

    // The outcome of a service call. Controllers turn it into a response:
    // NotFound -> 404, Forbidden -> 403, Invalid -> form errors, Ok -> Message as a toast.
    public class ServiceResult
    {
        public ResultStatus Status { get; protected set; } = ResultStatus.Ok;
        public string? Message { get; protected set; }

        // Field name -> message ("" means the whole form)
        public List<KeyValuePair<string, string>> Errors { get; } = new();

        public bool Succeeded => Status == ResultStatus.Ok;

        public ServiceResult AddError(string field, string message)
        {
            Status = ResultStatus.Invalid;
            Message ??= message;
            Errors.Add(new(field, message));
            return this;
        }

        public static ServiceResult Ok(string? message = null) => new() { Message = message };
        public static ServiceResult NotFound() => new() { Status = ResultStatus.NotFound };
        public static ServiceResult Forbidden() => new() { Status = ResultStatus.Forbidden };
        public static ServiceResult Invalid(string message, string field = "") => new ServiceResult().AddError(field, message);
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Value { get; private set; }

        public static ServiceResult<T> Success(T value, string? message = null) => new() { Value = value, Message = message };
        public static new ServiceResult<T> NotFound() => new() { Status = ResultStatus.NotFound };
        public static new ServiceResult<T> Forbidden() => new() { Status = ResultStatus.Forbidden };

        public static new ServiceResult<T> Invalid(string message, string field = "")
        {
            var result = new ServiceResult<T>();
            result.AddError(field, message);
            return result;
        }
    }
}
