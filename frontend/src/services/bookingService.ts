// Booking service to handle booking operations
import { DataSourceAdapter } from './dataSourceAdapter';
import { CreateBookingCommand, Booking, BookingStatus, BookingWithDetails } from '../types/api';

export interface BookingDetails {
    serviceId: string;
    date: Date;
    timeSlot: string;
    clientName: string;
    clientPhone: string;
    clientEmail: string;
    providerName?: string;
}

export const createBooking = async (bookingDetails: BookingDetails): Promise<Booking> => {
    // Convert date and timeSlot to startTime and endTime
    const startTime = new Date(bookingDetails.date);
    const [hours, minutes] = bookingDetails.timeSlot.split(':').map(Number);
    startTime.setHours(hours, minutes, 0, 0);

    // Assume 1 hour duration for now - this should come from the service
    const endTime = new Date(startTime);
    endTime.setHours(endTime.getHours() + 1);

    const createBookingCommand: CreateBookingCommand = {
        serviceId: bookingDetails.serviceId,
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString(),
        providerName: bookingDetails.providerName
    };

    const response = await DataSourceAdapter.createBooking(createBookingCommand);

    if (response.error) {
        throw new Error(response.error);
    }

    if (!response.data) {
        throw new Error('No booking data returned');
    }

    return response.data;
};

export const getBooking = async (bookingId: string): Promise<Booking | null> => {
    const response = await DataSourceAdapter.getBooking(bookingId);

    if (response.error) {
        throw new Error(response.error);
    }

    // Return null if booking not found instead of throwing error
    return response.data || null;
};

export const updateBookingStatus = async (
    bookingId: string,
    status: BookingStatus
): Promise<Booking | null> => {
    const response = await DataSourceAdapter.updateBookingStatus(bookingId, status);

    if (response.error) {
        throw new Error(response.error);
    }

    // Return null if booking not found instead of throwing error
    return response.data || null;
};

// Get bookings for a provider with enhanced details
export const getProviderBookings = async (
    providerId: string,
    status?: BookingStatus,
    startDate?: string,
    endDate?: string,
    isClient?: boolean
): Promise<BookingWithDetails[]> => {
    try {
        const response = await DataSourceAdapter.getProviderBookings(providerId, status, startDate, endDate, isClient);

        if (response.error) {
            throw new Error(response.error);
        }

        return response.data || [];
    } catch (error) {
        console.error('Get provider bookings failed:', error);
        throw error;
    }
};

// Get bookings for a client
export const getClientBookings = async (clientId: string): Promise<BookingWithDetails[]> => {
    try {
        const response = await DataSourceAdapter.getClientBookings(clientId);

        if (response.error) {
            throw new Error(response.error);
        }

        return response.data || [];
    } catch (error) {
        console.error('Get client bookings failed:', error);
        throw error;
    }
}