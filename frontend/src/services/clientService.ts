// Client service to fetch client data from configured data source
import { DataSourceAdapter } from './dataSourceAdapter';
import { Client } from '../types/api';

export const getClients = async (): Promise<Client[]> => {
    const response = await DataSourceAdapter.getClients();

    if (response.error) {
        throw new Error(response.error);
    }

    return response.data || [];
};
