/** Mirrors PixDynamicGallery.Domain.Enums.FinanceTransactionType exactly — numeric values matter (see PhotoStatus for the established convention). */
export enum FinanceTransactionType {
  Income = 0,
  Expense = 1,
}

/** Mirrors PixDynamicGallery.Domain.Enums.FinanceCategory exactly — numeric values matter. */
export enum FinanceCategory {
  Payment = 0,
  Tip = 1,
  Photos = 2,
  Gasoline = 3,
  Paper = 4,
  Equipment = 5,
  Marketing = 6,
  Other = 7,
  Usb = 8,
}

export const INCOME_CATEGORIES: FinanceCategory[] = [FinanceCategory.Payment, FinanceCategory.Tip, FinanceCategory.Other];
export const EXPENSE_CATEGORIES: FinanceCategory[] = [
  FinanceCategory.Photos,
  FinanceCategory.Usb,
  FinanceCategory.Gasoline,
  FinanceCategory.Paper,
  FinanceCategory.Equipment,
  FinanceCategory.Marketing,
  FinanceCategory.Other,
];

export const FINANCE_CATEGORY_LABELS: Record<FinanceCategory, string> = {
  [FinanceCategory.Payment]: 'Pago',
  [FinanceCategory.Tip]: 'Propina',
  [FinanceCategory.Photos]: 'Fotos',
  [FinanceCategory.Gasoline]: 'Gasolina',
  [FinanceCategory.Paper]: 'Papel',
  [FinanceCategory.Equipment]: 'Equipo',
  [FinanceCategory.Marketing]: 'Marketing',
  [FinanceCategory.Other]: 'Otro',
  [FinanceCategory.Usb]: 'USB',
};

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.EventTransactionDto. */
export interface EventTransactionDto {
  id: string;
  eventId: string;
  type: FinanceTransactionType;
  category: FinanceCategory;
  description: string | null;
  amount: number;
  transactionDate: string;
  isAutoCalculated: boolean;
  photoCount: number | null;
  costPerPhotoSnapshot: number | null;
  distanceKm: number | null;
  costPerKmSnapshot: number | null;
  usbCount: number | null;
  costPerUsbSnapshot: number | null;
}

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.EventFinanceSummaryDto. */
export interface EventFinanceSummaryDto {
  eventId: string;
  transactions: EventTransactionDto[];
  totalIncome: number;
  totalExpense: number;
  profit: number;
  suggestedPhotoCount: number;
  suggestedCostPerPhoto: number;
  suggestedCostPerKm: number;
  suggestedUsbCount: number;
  suggestedCostPerUsb: number;
}

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.GlobalExpenseDto. */
export interface GlobalExpenseDto {
  id: string;
  expenseDate: string;
  category: FinanceCategory;
  description: string;
  amount: number;
}

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.FinanceSettingsDto. */
export interface FinanceSettingsDto {
  costPerKm: number;
}

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.FinanceDashboardDto. */
export interface FinanceDashboardDto {
  totalIncome: number;
  totalRealExpenses: number;
  netProfit: number;
  perEventBreakdown: PerEventBreakdownDto[];
  /** Sum of every agenda deposit not yet transferred to an event — already folded into totalIncome, broken out here for visibility. */
  pendingDepositsTotal: number;
  agendaDeposits: PendingAgendaDepositDto[];
  incomeByMonth: MonthlyAmountDto[];
  eventsByMonth: MonthlyCountDto[];
}

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.MonthlyAmountDto. */
export interface MonthlyAmountDto {
  year: number;
  month: number;
  total: number;
}

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.MonthlyCountDto. */
export interface MonthlyCountDto {
  year: number;
  month: number;
  count: number;
}

/** Mirrors PixDynamicGallery.Application.Finance.Dtos.PendingAgendaDepositDto. */
export interface PendingAgendaDepositDto {
  agendaEntryId: string;
  clientName: string;
  eventDate: string;
  total: number;
}

export interface PerEventBreakdownDto {
  eventId: string;
  eventName: string;
  income: number;
  expense: number;
  profit: number;
}
