
namespace QLNS.FullNet.Data.Entities
{
    public class ProfileUpdateRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public string? NewPhoneNumber { get; set; }
        public DateTime? NewDateOfBirth { get; set; }
        public string Status { get; set; } = "Pending"; // Trạng thái: Pending, Approved, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}