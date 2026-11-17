using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Apitest.Dtos;

public class PatchUserDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? Login { get; set; }

    [StringLength(128, MinimumLength = 8)]
    public string? Password { get; set; }

    [Range(1, int.MaxValue)]
    public int? RoleId { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? UserName { get; set; }

    public bool? IsActive { get; set; }
}
