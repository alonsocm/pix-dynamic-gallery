/** Mirrors PixDynamicGallery.Domain.Enums.AgendaStatus exactly — numeric values matter (System.Text.Json serializes enums as numbers; see PhotoStatus for the established convention). */
export enum AgendaStatus {
  Prospect = 0,
  Confirmed = 1,
  Completed = 2,
  Cancelled = 3,
}

export const AGENDA_STATUSES: AgendaStatus[] = [
  AgendaStatus.Prospect,
  AgendaStatus.Confirmed,
  AgendaStatus.Completed,
  AgendaStatus.Cancelled,
];

export const AGENDA_STATUS_LABELS: Record<AgendaStatus, string> = {
  [AgendaStatus.Prospect]: 'Prospecto',
  [AgendaStatus.Confirmed]: 'Confirmado',
  [AgendaStatus.Completed]: 'Completado',
  [AgendaStatus.Cancelled]: 'Cancelado',
};

/** Mirrors PixDynamicGallery.Application.Agenda.Dtos.AgendaEntryDto. */
export interface AgendaEntryDto {
  id: string;
  clientName: string;
  contactPhone: string | null;
  contactEmail: string | null;
  eventType: string;
  eventDate: string;
  location: string | null;
  notes: string | null;
  agreedPrice: number | null;
  status: AgendaStatus;
  linkedEventId: string | null;
  createdAtUtc: string;
}

/** Mirrors AgendaController's CreateAgendaEntryCommand/UpdateAgendaEntryRequest request body. */
export interface AgendaEntryRequest {
  clientName: string;
  eventType: string;
  eventDate: string;
  contactPhone?: string | null;
  contactEmail?: string | null;
  location?: string | null;
  notes?: string | null;
  agreedPrice?: number | null;
}
