namespace PixDynamicGallery.Application.Common.Exceptions;

/// <summary>
/// Thrown by a handler when a requested entity does not exist. Mapped to HTTP 404 by the API's
/// exception-handling middleware.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
    }
}
