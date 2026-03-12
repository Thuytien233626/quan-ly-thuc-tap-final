namespace DNC.InternshipSystem.Core.Enums
{
    /// <summary>
    /// Trang thai don dang ky thuc tap
    /// </summary>
    public enum RegistrationStatus
    {
        /// <summary>
        /// Cho duyet
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Da duyet
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Tu choi
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Hoan thanh
        /// </summary>
        Completed = 3
    }
}
