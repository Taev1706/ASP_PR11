using System.ComponentModel.DataAnnotations;

namespace Apitest.Dtos;

public class RegisterDto
{
    [Required]
    [StringLength(100)]
    public string Login { get; set; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = null!;
}
