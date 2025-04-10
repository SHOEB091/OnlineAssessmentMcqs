public enum UserRole
{
    Admin,
    Organization,
    Candidate
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }  // ✅ Enum instead of string
}
