/** Mirrors PixDynamicGallery.Application.Inventory.Dtos.PaperPurchaseDto. */
export interface PaperPurchaseDto {
  id: string;
  purchaseDate: string;
  sheetsCount: number;
  totalCost: number;
  costPerSheet: number;
  notes: string | null;
}

/** Mirrors PixDynamicGallery.Application.Inventory.Dtos.PaperStockDto. */
export interface PaperStockDto {
  purchases: PaperPurchaseDto[];
  totalPurchasedSheets: number;
  totalConsumedSheets: number;
  remainingSheets: number;
  suggestedCostPerPhoto: number;
}

/** Mirrors PixDynamicGallery.Application.Inventory.Dtos.UsbPurchaseDto. */
export interface UsbPurchaseDto {
  id: string;
  purchaseDate: string;
  unitsCount: number;
  totalCost: number;
  costPerUnit: number;
  notes: string | null;
}

/** Mirrors PixDynamicGallery.Application.Inventory.Dtos.UsbStockDto. */
export interface UsbStockDto {
  purchases: UsbPurchaseDto[];
  totalPurchasedUnits: number;
  totalConsumedUnits: number;
  remainingUnits: number;
  suggestedCostPerUsb: number;
}
