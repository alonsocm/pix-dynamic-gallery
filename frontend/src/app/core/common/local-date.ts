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

/**
 * Converts an `<input type="datetime-local">` value ("YYYY-MM-DDTHH:mm") into an ISO instant.
 *
 * A bare `new Date("2027-02-06T18:00")` already parses as **local** time per spec (unlike the
 * date-only case above), so this just documents that and gives the round trip a named,
 * symmetric counterpart to {@link isoToLocalDateTimeInput} instead of leaving the conversion
 * implicit at each call site.
 */
export function localDateTimeInputToIso(dateTimeLocal: string): string {
  return new Date(dateTimeLocal).toISOString();
}

/**
 * Converts an ISO instant back into the local "YYYY-MM-DDTHH:mm" string a
 * `<input type="datetime-local">` needs for its value — confirmed live: prefilling the agenda's
 * edit form with `isoInstant.slice(0, 16)` fed the instant's **UTC** digits straight into the
 * input as if they were already local wall-clock time, so a booking saved as 6pm reopened showing
 * a different day/time in any timezone behind UTC (e.g. `America/Mexico_City`, UTC-6). Building
 * the string from the `Date` object's local getters instead anchors it to the timezone the
 * browser is actually in, so the reopened form shows what the user picked.
 */
export function isoToLocalDateTimeInput(iso: string): string {
  const date = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
