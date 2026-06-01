using System.ComponentModel.DataAnnotations;

namespace QLNS.FullNet.Data.Entities;

public class Department
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên phòng ban không được để trống")]
    [Display(Name = "Tên phòng ban")]
    public string Name { get; set; } = string.Empty;
}
