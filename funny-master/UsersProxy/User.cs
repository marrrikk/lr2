using System.Text.Json;
using System.Text.Json.Serialization;

namespace UsersProxy;

public class User
{
    public string Id { get; set; } = "";
    public string NewId { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string Surname { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string Organization { get; set; } = "";
    public string[] Roles { get; set; } = [];
    public bool Archive { get; set; }
    public bool Blocked { get; set; }

    // Сохраняем остальные поля сервиса при обновлении пользователя.
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraFields { get; set; } = new();

    public bool IsDeleted() => ExtraFields.Any(field =>
        (field.Key.Equals("deleted", StringComparison.OrdinalIgnoreCase) ||
         field.Key.Equals("isDeleted", StringComparison.OrdinalIgnoreCase)) &&
        field.Value.ValueKind == JsonValueKind.True);
}
