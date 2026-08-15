const dateParts = (instant: number, timeZone: string) => {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone, year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23',
  }).formatToParts(new Date(instant));
  const value = (type: Intl.DateTimeFormatPartTypes) => Number(parts.find((part) => part.type === type)?.value);
  return { year: value('year'), month: value('month'), day: value('day'), hour: value('hour'), minute: value('minute'), second: value('second') };
};

const localMidnight = (date: string, timeZone: string): number => {
  const [year, month, day] = date.split('-').map(Number);
  const target = Date.UTC(year, month - 1, day);
  let instant = target;
  for (let attempt = 0; attempt < 3; attempt += 1) {
    const parts = dateParts(instant, timeZone);
    const representedAsUtc = Date.UTC(parts.year, parts.month - 1, parts.day, parts.hour, parts.minute, parts.second);
    instant = target - (representedAsUtc - instant);
  }
  return instant;
};

const wholeSeconds = (instant: number) => new Date(instant).toISOString().replace('.000Z', 'Z');

export const businessDate = (timeZone: string, instant = new Date()): string => {
  const parts = dateParts(instant.getTime(), timeZone);
  return `${parts.year}-${String(parts.month).padStart(2, '0')}-${String(parts.day).padStart(2, '0')}`;
};

export const businessTimeLabel = (instant: string, timeZone: string): string => new Intl.DateTimeFormat('en-ZA', {
  timeZone, hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
}).format(new Date(instant));

export const businessDateTimeLabel = (instant: string, timeZone: string): string => new Intl.DateTimeFormat('en-ZA', {
  timeZone, dateStyle: 'medium', timeStyle: 'short', hourCycle: 'h23',
}).format(new Date(instant));

export const businessDayRange = (date: string, timeZone: string) => {
  const [year, month, day] = date.split('-').map(Number);
  const following = new Date(Date.UTC(year, month - 1, day + 1)).toISOString().slice(0, 10);
  return { from: wholeSeconds(localMidnight(date, timeZone)), to: wholeSeconds(localMidnight(following, timeZone)) };
};
