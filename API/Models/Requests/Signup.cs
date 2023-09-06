using System.ComponentModel.DataAnnotations;
using ShockLink.API.DataAnnotations;

namespace ShockLink.API.Models.Requests;

public class Signup
{
    [Username(true)]
    public required string Username { get; set; }
    [StringLength(256, MinimumLength = 12)]
    public required string Password { get; set; }
    [EmailAddress]
    public required string Email { get; set; }
}