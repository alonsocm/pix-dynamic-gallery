/** Mirrors PixDynamicGallery.Application.Common.Models.PaginatedList<T> (wire shape, camelCase). */
export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
