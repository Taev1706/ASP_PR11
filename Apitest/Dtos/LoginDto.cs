using System.ComponentModel.DataAnnotations;

namespace Apitest.Dtos;

public class LoginDto
{
    [Required]
    [StringLength(100)]
    public string Login { get; set; } = null!;

    [Required]
    [StringLength(128)]
    public string Password { get; set; } = null!;
}
