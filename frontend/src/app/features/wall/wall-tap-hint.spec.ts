import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EventDto } from '../../core/models/event.model';
import { PhotoDto, PhotoStatus } from '../../core/models/photo.model';
import { PhotoUploadedNotification } from '../../core/models/signalr-notifications.model';
import { SignalRService } from '../../core/signalr/signalr.service';
import { PhotoGridItemComponent } from './photo-grid-item.component';
import { WallComponent } from './wall.component';
import { WallPhotosService } from './wall-photos.service';

const HINT = 'Toca tu foto para verla y descargarla en HD';

const event: EventDto = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Boda de Ana y Luis',
  slug: 'boda-ana',
  guestBaseUrl: 'http://localhost:4200',
  isActive: true,
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  photoCount: 1,
};

function samplePhoto(id = 'photo-1'): PhotoDto {
  return {
    id,
    eventId: event.id,
    fileName: 'IMG_0001.jpg',
    url: `https://img.example/${id}.jpg`,
    thumbnailUrl: `https://img.example/${id}-thumb.jpg`,
    contentType: 'image/jpeg',
    sizeBytes: 120_000,
    status: PhotoStatus.Uploaded,
    capturedAtUtc: '2026-01-01T00:00:00.000Z',
    uploadedAtUtc: '2026-01-01T00:00:01.000Z',
  };
}

/** WallPhotosService is provided on the component, so the test replaces that provider outright. */
class WallPhotosStub {
  readonly photos = signal<PhotoDto[]>([]);
  readonly pageNumber = signal(0);
  readonly hasNextPage = signal(false);
  readonly loading = signal(false);
  readonly loadInitial = vi.fn(async () => {});
  readonly loadNextPage = vi.fn(async () => {});
  readonly prependRealtime = vi.fn();
}

class SignalRStub {
  readonly connectionState = signal('disconnected');
  readonly lastPhotoUploaded = signal<PhotoUploadedNotification | null>(null);
  readonly lastPhotoFailed = signal(null);
  readonly activeEventId = signal<string | null>(null);
  readonly connect = vi.fn(async () => {});
  readonly joinEvent = vi.fn(async () => {});
  readonly leaveEvent = vi.fn(async () => {});
}

describe('wall tap hint', () => {
  let wallPhotos: WallPhotosStub;
  let signalR: SignalRStub;

  beforeEach(() => {
    vi.stubGlobal(
      'IntersectionObserver',
      class {
        observe(): void {}
        unobserve(): void {}
        disconnect(): void {}
        takeRecords(): [] {
          return [];
        }
      },
    );
    vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
    wallPhotos = new WallPhotosStub();
    signalR = new SignalRStub();

    TestBed.configureTestingModule({
      imports: [WallComponent],
      providers: [{ provide: SignalRService, useValue: signalR }],
    });
    TestBed.overrideComponent(WallComponent, {
      set: { providers: [{ provide: WallPhotosService, useValue: wallPhotos }] },
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  async function renderWall(): Promise<ComponentFixture<WallComponent>> {
    const fixture = TestBed.createComponent(WallComponent);
    fixture.componentRef.setInput('event', event);
    await fixture.whenStable();
    return fixture;
  }

  it('pins the download hint above the photos, clear of the safe area', async () => {
    wallPhotos.photos.set([samplePhoto()]);
    const fixture = await renderWall();
    const host: HTMLElement = fixture.nativeElement;

    const hint = host.querySelector('[role="note"]');
    expect(hint?.textContent).toContain(HINT);

    const header = hint?.closest('header');
    expect(header?.className).toContain('sticky');
    expect(header?.className).toContain('safe-area-inset-top');
    expect(header?.className).toContain('safe-area-inset-left');
    expect(header?.className).toContain('safe-area-inset-right');

    const grid = host.querySelector('.columns-2');
    expect(grid).toBeTruthy();
    expect(header?.contains(grid!)).toBe(false);
    expect(header!.compareDocumentPosition(grid!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    const page = grid?.parentElement;
    expect(page?.className).toContain('safe-area-inset-bottom');
  });

  it('shows the hint even when the wall has no photos yet', async () => {
    const fixture = await renderWall();
    expect(fixture.nativeElement.textContent).toContain(HINT);
    expect(fixture.nativeElement.textContent).toContain('Aún no hay fotos');
  });

  it('gives every tile a tappable HD affordance', async () => {
    const fixture = TestBed.createComponent(PhotoGridItemComponent);
    fixture.componentRef.setInput('photo', samplePhoto());
    await fixture.whenStable();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-label')).toBe('Ver foto en HD');
    expect(button.className).toContain('cursor-pointer');

    const badge = button.querySelector('span[aria-hidden="true"] svg');
    expect(badge).toBeTruthy();
    expect(button.contains(badge)).toBe(true);
  });
});
