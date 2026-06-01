namespace QLNS.FullNet.Data.Entities; 

public class AppUser 
{ 
    public int Id { get; set; } 
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee"; // Default role
}
