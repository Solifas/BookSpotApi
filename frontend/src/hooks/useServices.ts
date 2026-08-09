// Custom hook for service management using TanStack Query
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { SearchService } from '../services/searchService';
import { BusinessService } from '../services/businessService';
import { ServiceSearchParams, CreateServiceCommand, UpdateServiceCommand } from '../types/api';

// Query keys for consistent caching
export const serviceKeys = {
    all: ['services'] as const,
    search: (params: ServiceSearchParams) => [...serviceKeys.all, 'search', params] as const,
    business: (businessId: string) => [...serviceKeys.all, 'business', businessId] as const,
    detail: (id: string) => [...serviceKeys.all, 'detail', id] as const,
};

// Hook for searching services
export const useSearchServices = (params: ServiceSearchParams) => {
    return useQuery({
        queryKey: serviceKeys.search(params),
        queryFn: () => SearchService.searchServices(params),
        staleTime: 5 * 60 * 1000, // 5 minutes
        enabled: Object.keys(params).length > 0, // Only run if we have search params
    });
};

// Hook for getting services by business
export const useBusinessServices = (businessId: string) => {
    return useQuery({
        queryKey: serviceKeys.business(businessId),
        queryFn: () => SearchService.getServicesByBusiness(businessId),
        enabled: !!businessId,
    });
};

// Hook for creating a service
export const useCreateService = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (data: Omit<CreateServiceCommand, 'isActive'>) =>
            BusinessService.createService(data),
        onSuccess: (newService) => {
            // Invalidate and refetch business services
            queryClient.invalidateQueries({ queryKey: serviceKeys.business(newService.businessId) });
            // Invalidate search results
            queryClient.invalidateQueries({ queryKey: serviceKeys.all });
        },
    });
};

// Hook for updating a service
export const useUpdateService = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ serviceId, updates }: { serviceId: string; updates: Partial<UpdateServiceCommand> }) =>
            BusinessService.updateService(serviceId, updates),
        onSuccess: (updatedService) => {
            // Update the specific service in cache
            queryClient.setQueryData(serviceKeys.detail(updatedService.id), updatedService);
            // Invalidate business services
            queryClient.invalidateQueries({ queryKey: serviceKeys.business(updatedService.businessId) });
            // Invalidate search results
            queryClient.invalidateQueries({ queryKey: serviceKeys.all });
        },
    });
};

// Hook for deleting a service
export const useDeleteService = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (serviceId: string) => BusinessService.deleteService(serviceId),
        onSuccess: (_, serviceId) => {
            // Remove from all caches
            queryClient.invalidateQueries({ queryKey: serviceKeys.all });
        },
    });
};

// Hook for toggling service availability
export const useToggleServiceAvailability = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ serviceId, isActive }: { serviceId: string; isActive: boolean }) =>
            BusinessService.toggleServiceAvailability(serviceId, isActive),
        onSuccess: (updatedService) => {
            // Update caches
            queryClient.setQueryData(serviceKeys.detail(updatedService.id), updatedService);
            queryClient.invalidateQueries({ queryKey: serviceKeys.business(updatedService.businessId) });
        },
    });
};