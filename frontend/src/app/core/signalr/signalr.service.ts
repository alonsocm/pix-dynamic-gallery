import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AppConfigService } from '../config/app-config.service';
import { PhotoFailedNotification, PhotoUploadedNotification } from '../models/signalr-notifications.model';

export type SignalRConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/**
 * Single app-lifetime connection to EventHub (`/hubs/event`), shared by the kiosk and live wall
 * (the guest landing page doesn't need realtime per spec). Written as plain signals rather than
 * RxJS Subjects: the app is zoneless, and a signal write from any callback context — including a
 * raw SignalR `.on()` handler, which runs outside any Angular zone anyway — schedules a
 * re-render on its own, no `NgZone.run()` wrapping needed anywhere here.
 */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly config = inject(AppConfigService);

  private connection: signalR.HubConnection | null = null;
  private connectPromise: Promise<void> | null = null;

  readonly connectionState = signal<SignalRConnectionState>('disconnected');
  readonly lastPhotoUploaded = signal<PhotoUploadedNotification | null>(null);
  readonly lastPhotoFailed = signal<PhotoFailedNotification | null>(null);

  /** Which event's group we believe we're currently joined to — used to re-join after a reconnect. */
  readonly activeEventId = signal<string | null>(null);

  /** Idempotent: safe to call repeatedly (e.g. from every component that needs the hub). */
  connect(): Promise<void> {
    if (this.connectPromise) {
      return this.connectPromise;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.config.hubBaseUrl}/hubs/event`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('OnPhotoUploaded', (notification: PhotoUploadedNotification) => {
      this.lastPhotoUploaded.set(notification);
    });

    connection.on('OnPhotoFailed', (notification: PhotoFailedNotification) => {
      this.lastPhotoFailed.set(notification);
    });

    connection.onreconnecting(() => this.connectionState.set('reconnecting'));

    connection.onreconnected(() => {
      this.connectionState.set('connected');

      // SignalR groups are connection-scoped server-side (Groups.AddToGroupAsync keyed by
      // Context.ConnectionId in EventHub.cs) and a reconnect gets a brand-new ConnectionId —
      // without re-joining here, the client would silently stop receiving broadcasts.
      const eventId = this.activeEventId();
      if (eventId) {
        void connection.invoke('JoinEventGroup', eventId);
      }
    });

    connection.onclose(() => this.connectionState.set('disconnected'));

    this.connection = connection;
    this.connectionState.set('connecting');

    this.connectPromise = connection
      .start()
      .then(() => this.connectionState.set('connected'))
      .catch((error) => {
        this.connectionState.set('disconnected');
        this.connectPromise = null;
        throw error;
      });

    return this.connectPromise;
  }

  async joinEvent(eventId: string): Promise<void> {
    await this.connect();
    await this.connection!.invoke('JoinEventGroup', eventId);
    this.activeEventId.set(eventId);
  }

  async leaveEvent(eventId: string): Promise<void> {
    if (this.connection?.state !== signalR.HubConnectionState.Connected) {
      // Avoid throwing during fast unmount/navigation races where the connection is mid-teardown.
      return;
    }

    await this.connection.invoke('LeaveEventGroup', eventId);

    if (this.activeEventId() === eventId) {
      this.activeEventId.set(null);
    }
  }
}
