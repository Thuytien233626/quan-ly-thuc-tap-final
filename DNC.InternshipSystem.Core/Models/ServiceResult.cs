namespace DNC.InternshipSystem.Core.Models
{
    /// <summary>
    /// Ket qua tra ve chung cho tat ca Service
    /// Dung de thong nhat giao tiep giua Controller va Service Layer
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ServiceResult Ok(string message = "")
            => new() { Success = true, Message = message };

        public static ServiceResult Fail(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Ket qua tra ve co kem du lieu (Generic)
    /// Vi du: ServiceResult<Registration> tra ve kem thong tin don dang ky
    /// </summary>
    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data, string message = "")
            => new() { Success = true, Data = data, Message = message };

        public static new ServiceResult<T> Fail(string message)
            => new() { Success = false, Message = message };
    }
}
