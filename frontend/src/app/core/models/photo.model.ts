/** Mirrors PixDynamicGallery.Domain.Enums.PhotoStatus exactly — numeric values matter. */
export enum PhotoStatus {
  Pending = 0,
  Uploading = 1,
  Uploaded = 2,
  Failed = 3,
}

/** Mirrors PixDynamicGallery.Application.Photos.Dtos.PhotoDto (wire shape, camelCase). */
export interface PhotoDto {
  id: string;
  eventId: string;
  fileName: string;
  url: string | null;
  contentType: string;
  sizeBytes: number;
  status: PhotoStatus;
  capturedAtUtc: string;
  uploadedAtUtc: string | null;
}
