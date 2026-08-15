import { describe, expect, it } from 'vitest';
import { businessDate, businessDateTimeLabel, businessDayRange, businessTimeLabel } from './businessTime';

describe('businessDayRange', () => {
  it('converts a Johannesburg business day to exact UTC boundaries', () => {
    expect(businessDayRange('2026-08-14', 'Africa/Johannesburg')).toEqual({
      from: '2026-08-13T22:00:00Z', to: '2026-08-14T22:00:00Z',
    });
  });

  it('uses the configured timezone offset rather than a hard-coded value', () => {
    expect(businessDayRange('2026-08-14', 'America/New_York')).toEqual({
      from: '2026-08-14T04:00:00Z', to: '2026-08-15T04:00:00Z',
    });
  });

  it('formats dates and slot labels in the business timezone', () => {
    expect(businessDate('America/New_York', new Date('2026-08-14T01:00:00Z'))).toBe('2026-08-13');
    expect(businessTimeLabel('2026-08-14T14:00:00Z', 'Africa/Johannesburg')).toBe('16:00');
    expect(businessDateTimeLabel('2026-08-14T14:00:00Z', 'Africa/Johannesburg')).toContain('16:00');
  });
});
