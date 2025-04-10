namespace OnlineAssessment.Web.Models
{
    public class RegisterRequest
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; } // Plain password input
        public required string Role { get; set; } // Role required
    }
}
