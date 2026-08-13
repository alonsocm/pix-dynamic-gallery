/** Mirrors PixDynamicGallery.Application.Events.Dtos.EventDto (wire shape, camelCase). */
export interface EventDto {
  id: string;
  name: string;
  slug: string;
  guestBaseUrl: string;
  isActive: boolean;
  createdAtUtc: string;
  photoCount: number;
}

/** Mirrors PixDynamicGallery.Application.Events.Dtos.AdminEventDto — admin-only, never returned by the public GetBySlug endpoint. */
export interface AdminEventDto extends EventDto {
  watchFolderPath: string;
}

/** Mirrors PixDynamicGallery.Application.Events.Commands.CreateEvent.CreateEventCommand (request body). */
export interface CreateEventRequest {
  name: string;
  slug: string;
  watchFolderPath: string;
  guestBaseUrl: string;
}

/** Mirrors ASP.NET Core's ValidationProblemDetails — { fieldName: [error, ...] }. */
export interface ValidationProblemDetails {
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
