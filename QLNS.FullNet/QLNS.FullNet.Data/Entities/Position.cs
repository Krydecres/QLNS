using System.ComponentModel.DataAnnotations;

namespace QLNS.FullNet.Data.Entities;
public class Position
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên chức vụ không được để trống")]
    [Display(Name = "Tên chức vụ")]
    public string Name { get; set; } = string.Empty;
}
