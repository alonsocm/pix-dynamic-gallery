/**
 * Converts a date-only `<input type="date">` value ("YYYY-MM-DD") into an ISO instant that
 * survives the round trip to the API and back without shifting a day.
 *
 * `new Date("2026-09-07").toISOString()` is a real bug, not a style nit: the date-only ISO format
 * is parsed as **UTC midnight**, so in any timezone behind UTC (e.g. `America/Mexico_City`,
 * UTC-6) the resulting instant is 6pm the *previous* day in local time. Every screen that later
 * renders that instant with `new Date(iso).toLocaleDateString(...)` (which localizes) then shows
 * the wrong calendar day — confirmed live: picking "7 sep" in the finance dashboard's expense form
 * saved and redisplayed as "6 sep". Parsing the same string as *local* date components instead
 * (`new Date(year, month - 1, day)`, the constructor overload that takes numeric parts) anchors it
 * to local midnight, so the round trip through the API and back to `toLocaleDateString` always
 * reproduces the calendar day the user actually picked.
 */
export function localDateInputToIso(dateOnly: string): string {
  const [year, month, day] = dateOnly.split('-').map(Number);
  return new Date(year, month - 1, day).toISOString();
}
