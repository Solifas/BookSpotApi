// Custom hook for booking management using TanStack Query
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
    createBooking,
    getBooking,
    updateBookingStatus,
    getProviderBookings,
    getClientBookings,
    BookingDetails
} from '../services/bookingService';
import { BookingStatus, Booking, BookingWithDetails } from '../types/api';

// Query keys for consistent caching
export const bookingKeys = {
    all: ['bookings'] as const,
    detail: (id: string) => [...bookingKeys.all, 'detail', id] as const,
    provider: (providerId: string, status?: BookingStatus) =>
        [...bookingKeys.all, 'provider', providerId, status] as const,
    client: (clientId: string) => [...bookingKeys.all, 'client', clientId] as const,
};

// Hook for getting a specific booking
export const useBooking = (bookingId: string) => {
    return useQuery({
        queryKey: bookingKeys.detail(bookingId),
        queryFn: () => getBooking(bookingId),
        enabled: !!bookingId,
    });
};

// Hook for getting provider bookings
export const useProviderBookings = (
    providerId: string,
    status?: BookingStatus,
    startDate?: string,
    endDate?: string,
    isClient?: boolean
) => {
    return useQuery({
        queryKey: bookingKeys.provider(providerId, status),
        queryFn: () => getProviderBookings(providerId, status, startDate, endDate, isClient),
        enabled: !!providerId,
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
};

// Hook for getting provider bookings by date range
export const useProviderBookingsByDateRange = (
    providerId: string,
    startDate: Date,
    endDate: Date,
    status?: BookingStatus
) => {
    return useProviderBookings(
        providerId,
        status,
        startDate.toISOString().split('T')[0],
        endDate.toISOString().split('T')[0]
    );
};

// Hook for getting today's bookings
export const useTodayBookings = (providerId: string) => {
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    return useProviderBookingsByDateRange(providerId, today, tomorrow);
};

// Hook for getting client bookings
export const useClientBookings = (clientId: string) => {
    return useQuery({
        queryKey: bookingKeys.client(clientId),
        queryFn: () => getClientBookings(clientId),
        enabled: !!clientId,
    });
};

// Hook for creating a booking
export const useCreateBooking = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (bookingDetails: BookingDetails) => createBooking(bookingDetails),
        onSuccess: (newBooking) => {
            // Invalidate provider and client bookings
            queryClient.invalidateQueries({ queryKey: bookingKeys.all });
        },
    });
};

// Hook for updating booking status
export const useUpdateBookingStatus = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ bookingId, status }: { bookingId: string; status: BookingStatus }) =>
            updateBookingStatus(bookingId, status),
        onSuccess: (updatedBooking) => {
            // Only update cache if booking was found and updated
            if (updatedBooking) {
                // Update the specific booking in cache
                queryClient.setQueryData(bookingKeys.detail(updatedBooking.id), updatedBooking);
            }
            // Always invalidate all booking lists to refresh data
            queryClient.invalidateQueries({ queryKey: bookingKeys.all });
        },
    });
};

// Convenience hooks for common booking actions
export const useAcceptBooking = () => {
    const updateStatus = useUpdateBookingStatus();

    return {
        ...updateStatus,
        acceptBooking: (bookingId: string) =>
            updateStatus.mutate({ bookingId, status: BookingStatus.CONFIRMED }),
    };
};

export const useDeclineBooking = () => {
    const updateStatus = useUpdateBookingStatus();

    return {
        ...updateStatus,
        declineBooking: (bookingId: string) =>
            updateStatus.mutate({ bookingId, status: BookingStatus.CANCELLED }),
    };
};

export const useCompleteBooking = () => {
    const updateStatus = useUpdateBookingStatus();

    return {
        ...updateStatus,
        completeBooking: (bookingId: string) =>
            updateStatus.mutate({ bookingId, status: BookingStatus.COMPLETED }),
    };
};