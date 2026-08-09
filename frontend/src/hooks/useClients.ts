// Hook for fetching clients using TanStack Query
import { useQuery } from '@tanstack/react-query';
import { getClients } from '../services/clientService';
import { Client } from '../types/api';

export const clientKeys = {
    all: ['clients'] as const,
};

export const useClients = () => {
    return useQuery<Client[]>({
        queryKey: clientKeys.all,
        queryFn: getClients,
        staleTime: 5 * 60 * 1000,
    });
};
