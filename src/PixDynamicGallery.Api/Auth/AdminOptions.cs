namespace PixDynamicGallery.Api.Auth;

/// <summary>Bound from the "Admin" config section. Empty Password (the default) makes <see cref="AdminAuthAttribute"/> a no-op.</summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    public string Password { get; set; } = string.Empty;
}
