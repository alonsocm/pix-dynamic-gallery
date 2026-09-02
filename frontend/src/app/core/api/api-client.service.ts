import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminAuthService } from '../admin/admin-auth.service';
import { AppConfigService } from '../config/app-config.service';
import { AgendaDepositDto, AgendaDepositRequest, AgendaEntryDto, AgendaEntryRequest, AgendaStatus } from '../models/agenda.model';
import { AdminEventDto, CreateEventRequest, EventDto } from '../models/event.model';
import {
  EventFinanceSummaryDto,
  EventTransactionDto,
  FinanceCategory,
  FinanceDashboardDto,
  FinanceSettingsDto,
  FinanceTransactionType,
  GlobalExpenseDto,
} from '../models/finance.model';
import { PaperPurchaseDto, PaperStockDto, UsbPurchaseDto, UsbStockDto } from '../models/inventory.model';
import { PaginatedList } from '../models/paginated-list.model';
import { PhotoDto } from '../models/photo.model';

export interface DeletePhotosResult {
  deletedCount: number;
  notFoundPhotoIds: string[];
}

/** Thin HttpClient wrapper over the REST surface exposed by EventsController/PhotosController. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);
  private readonly adminAuth = inject(AdminAuthService);

  /** GET /api/events/{slug} — slug is the human-readable event identifier used throughout the URLs. */
  getEvent(slug: string): Observable<EventDto> {
    return this.http.get<EventDto>(`${this.config.apiBaseUrl}/api/events/${encodeURIComponent(slug)}`);
  }

  /** POST /api/events — used by the admin "create event" screen. */
  createEvent(request: CreateEventRequest): Observable<EventDto> {
    return this.http.post<EventDto>(`${this.config.apiBaseUrl}/api/events`, request);
  }

  /** GET /api/events/{eventId}/photos — eventId here is the real Guid (EventDto.id), not the slug. */
  getEventPhotos(eventId: string, pageNumber = 1, pageSize = 30): Observable<PaginatedList<PhotoDto>> {
    return this.http.get<PaginatedList<PhotoDto>>(`${this.config.apiBaseUrl}/api/events/${eventId}/photos`, {
      params: { pageNumber, pageSize },
    });
  }

  /** GET /api/events/{eventId}/photos/{photoId} — both are Guids. */
  getPhoto(eventId: string, photoId: string): Observable<PhotoDto> {
    return this.http.get<PhotoDto>(`${this.config.apiBaseUrl}/api/events/${eventId}/photos/${photoId}`);
  }

  /** GET /api/events — admin-only; also doubles as AdminLoginComponent's "verify this password" call. */
  listEvents(): Observable<AdminEventDto[]> {
    return this.http.get<AdminEventDto[]>(`${this.config.apiBaseUrl}/api/events`, { headers: this.adminHeaders() });
  }

  /** PATCH /api/events/{eventId}/active — admin-only. */
  setEventActive(eventId: string, isActive: boolean): Observable<EventDto> {
    return this.http.patch<EventDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/active`,
      { isActive },
      { headers: this.adminHeaders() },
    );
  }

  /** POST /api/events/{eventId}/photos/delete — admin-only bulk hard-delete. */
  deletePhotos(eventId: string, photoIds: string[]): Observable<DeletePhotosResult> {
    return this.http.post<DeletePhotosResult>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/photos/delete`,
      { photoIds },
      { headers: this.adminHeaders() },
    );
  }

  // ---- Agenda (api/agenda) — all admin-only ----

  listAgendaEntries(filter?: { from?: string; to?: string; status?: AgendaStatus }): Observable<AgendaEntryDto[]> {
    const params: Record<string, string> = {};
    if (filter?.from) params['from'] = filter.from;
    if (filter?.to) params['to'] = filter.to;
    // `!== undefined`, not truthy — AgendaStatus.Prospect is 0, which is falsy.
    if (filter?.status !== undefined) params['status'] = String(filter.status);
    return this.http.get<AgendaEntryDto[]>(`${this.config.apiBaseUrl}/api/agenda`, { headers: this.adminHeaders(), params });
  }

  createAgendaEntry(request: AgendaEntryRequest): Observable<AgendaEntryDto> {
    return this.http.post<AgendaEntryDto>(`${this.config.apiBaseUrl}/api/agenda`, request, { headers: this.adminHeaders() });
  }

  updateAgendaEntry(id: string, request: AgendaEntryRequest): Observable<AgendaEntryDto> {
    return this.http.patch<AgendaEntryDto>(`${this.config.apiBaseUrl}/api/agenda/${id}`, request, { headers: this.adminHeaders() });
  }

  setAgendaStatus(id: string, status: AgendaStatus): Observable<AgendaEntryDto> {
    return this.http.patch<AgendaEntryDto>(`${this.config.apiBaseUrl}/api/agenda/${id}/status`, { status }, { headers: this.adminHeaders() });
  }

  linkAgendaEntryToEvent(id: string, eventId: string): Observable<AgendaEntryDto> {
    return this.http.post<AgendaEntryDto>(
      `${this.config.apiBaseUrl}/api/agenda/${id}/link-event`,
      { eventId },
      { headers: this.adminHeaders() },
    );
  }

  deleteAgendaEntry(id: string): Observable<void> {
    return this.http.delete<void>(`${this.config.apiBaseUrl}/api/agenda/${id}`, { headers: this.adminHeaders() });
  }

  /** Logs a deposit/advance payment for a booking — booked as income immediately if it's already linked to an Event. */
  addAgendaDeposit(agendaEntryId: string, request: AgendaDepositRequest): Observable<AgendaDepositDto> {
    return this.http.post<AgendaDepositDto>(
      `${this.config.apiBaseUrl}/api/agenda/${agendaEntryId}/deposits`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  /** Refused (400) once the deposit has been converted into an event income transaction. */
  deleteAgendaDeposit(agendaEntryId: string, depositId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.config.apiBaseUrl}/api/agenda/${agendaEntryId}/deposits/${depositId}`,
      { headers: this.adminHeaders() },
    );
  }

  // ---- Event finance (api/events/{eventId}/transactions) — all admin-only ----

  getEventFinanceSummary(eventId: string): Observable<EventFinanceSummaryDto> {
    return this.http.get<EventFinanceSummaryDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/transactions`,
      { headers: this.adminHeaders() },
    );
  }

  addEventTransaction(
    eventId: string,
    request: { type: FinanceTransactionType; category: FinanceCategory; description?: string | null; amount: number; transactionDate: string },
  ): Observable<EventTransactionDto> {
    return this.http.post<EventTransactionDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/transactions`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  addPhotoExpense(
    eventId: string,
    request: { photoCount?: number | null; costPerPhoto?: number | null; transactionDate?: string | null },
  ): Observable<EventTransactionDto> {
    return this.http.post<EventTransactionDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/transactions/photo-expense`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  addGasolineExpense(
    eventId: string,
    request: { distanceKm: number; costPerKm?: number | null; transactionDate?: string | null },
  ): Observable<EventTransactionDto> {
    return this.http.post<EventTransactionDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/transactions/gasoline-expense`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  addUsbExpense(
    eventId: string,
    request: { usbCount?: number | null; costPerUsb?: number | null; transactionDate?: string | null },
  ): Observable<EventTransactionDto> {
    return this.http.post<EventTransactionDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/transactions/usb-expense`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  updateEventTransaction(
    eventId: string,
    transactionId: string,
    request: { amount: number; description?: string | null; transactionDate: string },
  ): Observable<EventTransactionDto> {
    return this.http.patch<EventTransactionDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/transactions/${transactionId}`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  deleteEventTransaction(eventId: string, transactionId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/transactions/${transactionId}`,
      { headers: this.adminHeaders() },
    );
  }

  // ---- Studio-wide finance (api/finance) — all admin-only ----

  getFinanceDashboard(): Observable<FinanceDashboardDto> {
    return this.http.get<FinanceDashboardDto>(`${this.config.apiBaseUrl}/api/finance/dashboard`, { headers: this.adminHeaders() });
  }

  getFinanceSettings(): Observable<FinanceSettingsDto> {
    return this.http.get<FinanceSettingsDto>(`${this.config.apiBaseUrl}/api/finance/settings`, { headers: this.adminHeaders() });
  }

  updateFinanceSettings(costPerKm: number): Observable<FinanceSettingsDto> {
    return this.http.put<FinanceSettingsDto>(
      `${this.config.apiBaseUrl}/api/finance/settings`,
      { costPerKm },
      { headers: this.adminHeaders() },
    );
  }

  getGlobalExpenses(): Observable<GlobalExpenseDto[]> {
    return this.http.get<GlobalExpenseDto[]>(`${this.config.apiBaseUrl}/api/finance/global-expenses`, { headers: this.adminHeaders() });
  }

  addGlobalExpense(request: { expenseDate: string; category: FinanceCategory; description: string; amount: number }): Observable<GlobalExpenseDto> {
    return this.http.post<GlobalExpenseDto>(
      `${this.config.apiBaseUrl}/api/finance/global-expenses`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  deleteGlobalExpense(id: string): Observable<void> {
    return this.http.delete<void>(`${this.config.apiBaseUrl}/api/finance/global-expenses/${id}`, { headers: this.adminHeaders() });
  }

  // ---- Inventory (api/inventory) — all admin-only ----

  getPaperStock(): Observable<PaperStockDto> {
    return this.http.get<PaperStockDto>(`${this.config.apiBaseUrl}/api/inventory/paper-stock`, { headers: this.adminHeaders() });
  }

  addPaperPurchase(request: { purchaseDate: string; sheetsCount: number; totalCost: number; notes?: string | null }): Observable<PaperPurchaseDto> {
    return this.http.post<PaperPurchaseDto>(
      `${this.config.apiBaseUrl}/api/inventory/paper-purchases`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  deletePaperPurchase(id: string): Observable<void> {
    return this.http.delete<void>(`${this.config.apiBaseUrl}/api/inventory/paper-purchases/${id}`, { headers: this.adminHeaders() });
  }

  getUsbStock(): Observable<UsbStockDto> {
    return this.http.get<UsbStockDto>(`${this.config.apiBaseUrl}/api/inventory/usb-stock`, { headers: this.adminHeaders() });
  }

  addUsbPurchase(request: { purchaseDate: string; unitsCount: number; totalCost: number; notes?: string | null }): Observable<UsbPurchaseDto> {
    return this.http.post<UsbPurchaseDto>(
      `${this.config.apiBaseUrl}/api/inventory/usb-purchases`,
      request,
      { headers: this.adminHeaders() },
    );
  }

  deleteUsbPurchase(id: string): Observable<void> {
    return this.http.delete<void>(`${this.config.apiBaseUrl}/api/inventory/usb-purchases/${id}`, { headers: this.adminHeaders() });
  }

  private adminHeaders(): HttpHeaders {
    const password = this.adminAuth.password();
    return password ? new HttpHeaders({ 'X-Admin-Password': password }) : new HttpHeaders();
  }
}
