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
