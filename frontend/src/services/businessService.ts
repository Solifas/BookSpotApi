// Business management service
import { DataSourceAdapter } from './dataSourceAdapter';
import {
    Business,
    CreateBusinessCommand,
    UpdateBusinessCommand,
    Service,
    CreateServiceCommand,
    UpdateServiceCommand,
    UserType
} from '../types/api';

export class BusinessService {
    // Create a new business (providers only)
    static async createBusiness(businessData: Omit<CreateBusinessCommand, 'isActive'>): Promise<Business> {
        try {
            const command: CreateBusinessCommand = {
                ...businessData,
                isActive: true // Default to active
            };

            const response = await DataSourceAdapter.createBusiness(command);

            if (response.error) {
                throw new Error(response.error);
            }

            if (!response.data) {
                throw new Error('No business data returned');
            }

            return response.data;
        } catch (error) {
            console.error('Create business failed:', error);
            throw error;
        }
    }

    // Update business information
    static async updateBusiness(businessId: string, updates: Partial<UpdateBusinessCommand>): Promise<Business | null> {
        try {
            const command: UpdateBusinessCommand = {
                id: businessId,
                ...updates
            };

            const response = await DataSourceAdapter.updateBusiness(businessId, command);

            if (response.error) {
                throw new Error(response.error);
            }

            // Return null if business not found instead of throwing error
            return response.data || null;
        } catch (error) {
            console.error('Update business failed:', error);
            throw error;
        }
    }

    // Get business details
    static async getBusiness(businessId: string): Promise<Business | null> {
        try {
            const response = await DataSourceAdapter.getBusiness(businessId);

            if (response.error) {
                throw new Error(response.error);
            }

            // Return null if business not found instead of throwing error
            return response.data || null;
        } catch (error) {
            console.error('Get business failed:', error);
            throw error;
        }
    }

    // Create a service for a business
    static async createService(serviceData: Omit<CreateServiceCommand, 'isActive'>): Promise<Service> {
        try {
            const command: CreateServiceCommand = {
                ...serviceData,
                isActive: true // Default to active
            };

            const response = await DataSourceAdapter.createService(command);

            if (response.error) {
                throw new Error(response.error);
            }

            if (!response.data) {
                throw new Error('No service data returned');
            }

            return response.data;
        } catch (error) {
            console.error('Create service failed:', error);
            throw error;
        }
    }

    // Update a service
    static async updateService(serviceId: string, updates: Partial<UpdateServiceCommand>): Promise<Service | null> {
        try {
            const command: UpdateServiceCommand = {
                id: serviceId,
                ...updates
            };

            const response = await DataSourceAdapter.updateService(serviceId, command);

            if (response.error) {
                throw new Error(response.error);
            }

            // Return null if service not found instead of throwing error
            return response.data || null;
        } catch (error) {
            console.error('Update service failed:', error);
            throw error;
        }
    }

    // Delete a service
    static async deleteService(serviceId: string): Promise<void> {
        try {
            const response = await DataSourceAdapter.deleteService(serviceId);

            if (response.error) {
                throw new Error(response.error);
            }
        } catch (error) {
            console.error('Delete service failed:', error);
            throw error;
        }
    }

    // Get all services for a business
    static async getBusinessServices(businessId: string): Promise<Service[]> {
        try {
            const response = await DataSourceAdapter.getBusinessServices(businessId);

            if (response.error) {
                throw new Error(response.error);
            }

            return response.data || [];
        } catch (error) {
            console.error('Get business services failed:', error);
            throw error;
        }
    }

    // Toggle service availability
    static async toggleServiceAvailability(serviceId: string, isActive: boolean): Promise<Service> {
        return this.updateService(serviceId, { isActive });
    }

    // Toggle business availability
    static async toggleBusinessAvailability(businessId: string, isActive: boolean): Promise<Business> {
        return this.updateBusiness(businessId, { isActive });
    }
}