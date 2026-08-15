import { apiClient, createIdempotencyKey } from './api';
import type { BookingAction, BookingActionRequest, BookingDto, BookingMutationResultDto, BookingWithDetails, CreateBookingCommand } from '../types/api';

export interface BookingDetails {
  serviceId: string;
  date: Date;
  timeSlot: string;
  clientName: string;
  clientPhone: string;
  clientEmail: string;
  providerName?: string;
}

const pendingCreates = new Map<string, string>();
const pendingActions = new Map<string, { key: string; request: BookingActionRequest }>();
const isAmbiguous = (status: number) => status === 0 || status === 503;

export const toLegacyBooking = (booking: BookingDto): BookingWithDetails => ({
  id: booking.bookingId,
  serviceId: booking.serviceId,
  ...(booking.clientProfileId ? { clientId: booking.clientProfileId } : {}),
  providerId: booking.providerProfileId,
  providerName: booking.business.businessName,
  startTime: booking.startTime,
  endTime: booking.endTime,
  status: booking.status,
  createdAt: booking.createdAt,
  service: {
    id: booking.serviceId,
    businessId: booking.businessId,
    name: booking.service.name,
    price: booking.price.amount,
    durationMinutes: booking.service.durationMinutes,
  },
  ...(booking.client ? { client: {
    ...(booking.clientProfileId ? { id: booking.clientProfileId } : {}),
    fullName: booking.client.fullName,
    email: booking.client.email,
    contactNumber: booking.client.contactNumber ?? undefined,
  } } : {}),
  business: {
    id: booking.businessId,
    businessName: booking.business.businessName,
    city: booking.business.city,
  },
});

const throwResponseError = (response: { error?: string; status: number; code?: string }) => {
  const error = new Error(response.error || 'The request failed.') as Error & { status: number; code?: string };
  error.status = response.status;
  error.code = response.code;
  throw error;
};

export const createBooking = async (details: BookingDetails): Promise<BookingMutationResultDto> => {
  const start = new Date(details.date);
  const [hours, minutes] = details.timeSlot.split(':').map(Number);
  start.setHours(hours, minutes, 0, 0);
  const command: CreateBookingCommand = { serviceId: details.serviceId, startTime: start.toISOString().replace('.000Z', 'Z') };
  const operation = JSON.stringify(command);
  const key = pendingCreates.get(operation) ?? createIdempotencyKey();
  pendingCreates.set(operation, key);
  const response = await apiClient.createBooking(command, key);
  if (!isAmbiguous(response.status)) pendingCreates.delete(operation);
  if (response.error || !response.data) throwResponseError(response);
  return response.data;
};

export const getBooking = async (bookingId: string): Promise<BookingWithDetails | null> => {
  const response = await apiClient.getBooking(bookingId);
  if (response.status === 404) return null;
  if (response.error || !response.data) throwResponseError(response);
  return toLegacyBooking(response.data);
};

export const updateBookingStatus = async (bookingId: string, status: string): Promise<BookingWithDetails | null> => {
  const actions: Partial<Record<string, BookingAction>> = {
    confirmed: 'confirm', declined: 'decline', cancelled: 'cancel', completed: 'complete', no_show: 'mark_no_show',
  };
  const action = actions[status];
  if (!action) throw new Error('Unsupported booking status transition.');
  const operation = `${bookingId}:${action}`;
  let pending = pendingActions.get(operation);
  if (!pending) {
    const current = await apiClient.getBooking(bookingId);
    if (current.status === 404) return null;
    if (current.error || !current.data) throwResponseError(current);
    pending = { key: createIdempotencyKey(), request: { action, expectedVersion: current.data.version } };
    pendingActions.set(operation, pending);
  }
  const updated = await apiClient.performBookingAction(bookingId, pending.request, pending.key);
  if (!isAmbiguous(updated.status)) pendingActions.delete(operation);
  if (updated.error) throwResponseError(updated);
  const refreshed = await apiClient.getBooking(bookingId);
  if (refreshed.error || !refreshed.data) throwResponseError(refreshed);
  return toLegacyBooking(refreshed.data);
};

export const getProviderBookings = async (_subjectId: string, status?: string, from?: string, to?: string, isClient?: boolean): Promise<BookingWithDetails[]> => {
  const response = isClient ? await apiClient.getClientBookings() : await apiClient.getProviderBookings(undefined, status, from, to);
  if (response.error || !response.data) throwResponseError(response);
  return response.data.items.map(toLegacyBooking);
};

export const getClientBookings = async (_legacyClientId?: string): Promise<BookingWithDetails[]> => {
  const response = await apiClient.getClientBookings();
  if (response.error || !response.data) throwResponseError(response);
  return response.data.items.map(toLegacyBooking);
};
