using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixDynamicGallery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Intencionalmente vacía. EF Core la generó como un DropTable de las 7 tablas de
    /// Agenda/Finance/Inventory/EventTransaction — reales en Neon, pero ya no mapeadas aquí porque
    /// `pix-app` (repo separado, Cloudflare Workers) las administra ahora. Esta migración existe
    /// solo para que `ApplicationDbContextModelSnapshot.cs` quede sincronizado con el modelo
    /// reducido (Event/Photo) sin ejecutar ningún DDL real — aplicarla solo registra en
    /// `__EFMigrationsHistory` que el snapshot ya está al día, sin tocar ninguna tabla.
    /// </remarks>
    public partial class BaselineCabinOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op a propósito — ver el comentario de la clase.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op a propósito — ver el comentario de la clase.
        }
    }
}
