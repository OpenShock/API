﻿using System.ComponentModel.DataAnnotations;

namespace ShockLink.API.Models.Requests;

public class Login
{
    [MinLength(1)]
    public required string Password { get; set; }
    [MinLength(1)]
    public required string Email { get; set; }
}