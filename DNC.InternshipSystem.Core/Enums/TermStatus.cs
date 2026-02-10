namespace DNC.InternshipSystem.Core.Enums
{
    /// <summary>
    /// Trạng thái đợt thực tập
    /// </summary>
    public enum TermStatus
    {
        /// <summary>
        /// Ban nhap - Dang cau hinh
        /// </summary>
        Draft = 0,
        
        /// <summary>
        /// Dang mo dang ky
        /// </summary>
        Open = 1,
        
        /// <summary>
        /// Dang dien ra thuc tap
        /// </summary>
        InProgress = 2,
        
        /// <summary>
        /// Dang cham diem
        /// </summary>
        Grading = 3,
        
        /// <summary>
        /// Da hoan thanh
        /// </summary>
        Completed = 4
    }
}
