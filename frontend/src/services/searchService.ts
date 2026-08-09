// Enhanced search service for services and businesses
import { DataSourceAdapter } from './dataSourceAdapter';
import { ServiceSearchParams, ServiceSearchResponse, ServiceWithBusiness } from '../types/api';
import { Service as FrontendService } from '../data/servicesData';
import { adaptServiceWithBusinessToFrontend } from './serviceAdapter';

export class SearchService {
    // Search services with enhanced filtering
    static async searchServices(params: ServiceSearchParams): Promise<{
        services: FrontendService[];
        totalCount: number;
        page: number;
        pageSize: number;
        totalPages: number;
    }> {
        try {
            const response = await DataSourceAdapter.searchServices(params);

            if (response.error) {
                throw new Error(response.error);
            }

            const data = response.data;

            if (!data) {
                return {
                    services: [],
                    totalCount: 0,
                    page: 1,
                    pageSize: params.pageSize ?? 10,
                    totalPages: 0,
                };
            }

            const rawServices: Partial<ServiceWithBusiness>[] = Array.isArray(data)
                ? (data as ServiceWithBusiness[])
                : Array.isArray((data as ServiceSearchResponse).services)
                    ? (data as ServiceSearchResponse).services as ServiceWithBusiness[]
                    : [];

            const frontendServices = rawServices.map((service) =>
                adaptServiceWithBusinessToFrontend(service)
            );

            const totalCount = Array.isArray(data)
                ? rawServices.length
                : (data as ServiceSearchResponse).totalCount ?? rawServices.length;

            const pageSize = Array.isArray(data)
                ? params.pageSize ?? (rawServices.length || 10)
                : (data as ServiceSearchResponse).pageSize ?? params.pageSize ?? (rawServices.length || 10);

            const page = Array.isArray(data)
                ? params.page ?? 1
                : (data as ServiceSearchResponse).page ?? params.page ?? 1;

            const totalPages = pageSize > 0
                ? Math.max(1, Math.ceil((totalCount || rawServices.length) / pageSize))
                : 0;

            return {
                services: frontendServices,
                totalCount,
                page,
                pageSize,
                totalPages,
            };
        } catch (error) {
            console.error('Search services failed:', error);
            throw error;
        }
    }

    // Get services by business
    static async getServicesByBusiness(businessId: string): Promise<FrontendService[]> {
        try {
            const response = await DataSourceAdapter.getBusinessServices(businessId);

            if (response.error) {
                throw new Error(response.error);
            }

            if (!response.data) {
                return [];
            }

            // For now, we'll need to get business info separately
            // In a real implementation, you might want to create a new endpoint
            // that returns services with business info embedded
            const businessResponse = await DataSourceAdapter.getBusiness(businessId);
            const businessInfo = businessResponse.data;

            return response.data.map(service =>
                adaptApiServiceToFrontend(service, businessInfo ? {
                    name: businessInfo.businessName,
                    city: businessInfo.city,
                    address: businessInfo.address,
                    phone: businessInfo.phone,
                    email: businessInfo.email
                } : undefined)
            );
        } catch (error) {
            console.error('Get services by business failed:', error);
            throw error;
        }
    }

    // Helper method to build search parameters
    static buildSearchParams(
        query?: string,
        location?: string,
        category?: string,
        priceRange?: { min?: number; max?: number },
        durationRange?: { min?: number; max?: number },
        page: number = 1,
        pageSize: number = 10
    ): ServiceSearchParams {
        return {
            name: query,
            city: location,
            category,
            minPrice: priceRange?.min,
            maxPrice: priceRange?.max,
            minDuration: durationRange?.min,
            maxDuration: durationRange?.max,
            page,
            pageSize
        };
    }
}

// Import the adapter function
import { adaptApiServiceToFrontend } from './serviceAdapter';
