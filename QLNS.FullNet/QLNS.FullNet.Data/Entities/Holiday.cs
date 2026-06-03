using System.ComponentModel.DataAnnotations;

namespace QLNS.FullNet.Data.Entities;

public class Holiday
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên ngày nghỉ lễ không được để trống")]
    [Display(Name = "Tên ngày nghỉ lễ")]
    public string Name { get; set; } = null!;

    [Required]
    [Range(1, 12, ErrorMessage = "Tháng phải từ 1 đến 12")]
    [Display(Name = "Tháng")]
    public int Month { get; set; }

    [Required]
    [Range(1, 31, ErrorMessage = "Ngày phải từ 1 đến 31")]
    [Display(Name = "Ngày")]
    public int Day { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    /// <summary>
    /// Kiểm tra record có dữ liệu hợp lệ
    /// </summary>
    public bool IsValid => Month >= 1 && Month <= 12 && Day >= 1 && Day <= 31;

    /// <summary>
    /// Lấy ngày nghỉ lễ của năm hiện tại (hoặc năm chỉ định)
    /// </summary>
    public DateTime GetDateForYear(int year)
    {
        if (!IsValid)
            return DateTime.MinValue;
        int daysInMonth = DateTime.DaysInMonth(year, Month);
        int day = Math.Min(Day, daysInMonth);
        return new DateTime(year, Month, day);
    }

    /// <summary>
    /// Hiển thị ngày/tháng dạng dd/MM
    /// </summary>
    public string DateDisplay => $"{Day:D2}/{Month:D2}";
}
