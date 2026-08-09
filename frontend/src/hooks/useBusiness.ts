// Custom hook for business management using TanStack Query
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { BusinessService } from '../services/businessService';
import {
    Business,
    CreateBusinessCommand,
    UpdateBusinessCommand
} from '../types/api';

// Query keys for consistent caching
export const businessKeys = {
    all: ['businesses'] as const,
    detail: (id: string) => [...businessKeys.all, 'detail', id] as const,
    services: (id: string) => [...businessKeys.all, id, 'services'] as const,
};

// Hook for getting business details
export const useBusiness = (businessId: string) => {
    return useQuery({
        queryKey: businessKeys.detail(businessId),
        queryFn: () => BusinessService.getBusiness(businessId),
        enabled: !!businessId,
    });
};

// Hook for creating a business
export const useCreateBusiness = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (data: Omit<CreateBusinessCommand, 'isActive'>) =>
            BusinessService.createBusiness(data),
        onSuccess: (newBusiness) => {
            // Add to cache
            queryClient.setQueryData(businessKeys.detail(newBusiness.id), newBusiness);
            // Invalidate businesses list if we had one
            queryClient.invalidateQueries({ queryKey: businessKeys.all });
        },
    });
};

// Hook for updating a business
export const useUpdateBusiness = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ businessId, updates }: {
            businessId: string;
            updates: Partial<UpdateBusinessCommand>
        }) => BusinessService.updateBusiness(businessId, updates),
        onSuccess: (updatedBusiness) => {
            // Update the specific business in cache
            queryClient.setQueryData(businessKeys.detail(updatedBusiness.id), updatedBusiness);
        },
    });
};

// Hook for toggling business availability
export const useToggleBusinessAvailability = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ businessId, isActive }: { businessId: string; isActive: boolean }) =>
            BusinessService.toggleBusinessAvailability(businessId, isActive),
        onSuccess: (updatedBusiness) => {
            // Update cache
            queryClient.setQueryData(businessKeys.detail(updatedBusiness.id), updatedBusiness);
        },
    });
};

// Hook for getting business services (reusing from useServices)
export const useBusinessServicesQuery = (businessId: string) => {
    return useQuery({
        queryKey: businessKeys.services(businessId),
        queryFn: () => BusinessService.getBusinessServices(businessId),
        enabled: !!businessId,
    });
};