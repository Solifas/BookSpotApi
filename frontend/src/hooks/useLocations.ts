// Location-related hooks using TanStack Query
import { useQuery } from '@tanstack/react-query';
import { DataSourceAdapter } from '../services/dataSourceAdapter';
import { CityInfo } from '../types/api';

// Query keys for consistent caching
export const locationKeys = {
    all: ['locations'] as const,
    cities: () => [...locationKeys.all, 'cities'] as const,
};

// Hook for getting all available cities
export const useCities = () => {
    return useQuery({
        queryKey: locationKeys.cities(),
        queryFn: async () => {
            const response = await DataSourceAdapter.getCities();
            if (response.error) throw new Error(response.error);
            return response.data || [];
        },
        staleTime: 24 * 60 * 60 * 1000, // 24 hours - cities don't change often
        select: (data) => data || [], // Extract data from ApiResponse
    });
};

// Hook for getting formatted city suggestions for autocomplete
export const useCitySuggestions = (searchTerm: string) => {
    const { data: cities = [] } = useCities();

    const suggestions = searchTerm.length > 0
        ? cities
            .filter(city =>
                city.city.toLowerCase().includes(searchTerm.toLowerCase()) ||
                city.province.toLowerCase().includes(searchTerm.toLowerCase())
            )
            .map(city => `${city.city}, ${city.province}`)
            .slice(0, 10) // Limit to 10 suggestions
        : [];

    return suggestions;
};

// Hook for getting cities by province
export const useCitiesByProvince = (province?: string) => {
    const { data: cities = [] } = useCities();

    const filteredCities = province
        ? cities.filter(city =>
            city.province.toLowerCase() === province.toLowerCase()
        )
        : cities;

    return filteredCities;
};