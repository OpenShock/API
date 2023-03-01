using System.ComponentModel.DataAnnotations;

namespace ShockLink.API.Models.Requests;

public class Signup
{
    [StringLength(32, MinimumLength = 3)]
    public required string Username { get; set; }
    [StringLength(256, MinimumLength = 12)]
    public required string Password { get; set; }
    [EmailAddress]
    public required string Email { get; set; }
}