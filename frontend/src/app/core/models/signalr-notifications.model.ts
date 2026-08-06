/** Payload of the `OnPhotoUploaded` SignalR event (EventHub / PhotoNotifier, wire shape). */
export interface PhotoUploadedNotification {
  photoId: string;
  eventId: string;
  url: string;
  timestamp: string;
}

/** Payload of the `OnPhotoFailed` SignalR event (anonymous object server-side, same shape here). */
export interface PhotoFailedNotification {
  photoId: string;
  eventId: string;
  reason: string;
}
