namespace Apitest.Dtos;

public record LoginResponseDto(string AccessToken, int ExpiresInMinutes, int UserId, string Role);
