// Service adapter to bridge API data and frontend display data
import { Service as ApiService, ServiceWithBusiness } from '../types/api';
import { Service as FrontendService } from '../data/servicesData';

type BusinessDetails = {
    name?: string;
    city?: string;
    address?: string;
    phone?: string;
    email?: string;
    rating?: number;
    reviewCount?: number;
    providerName?: string;
};

const slugify = (value: string) => value.toLowerCase().replace(/\s+/g, '-');

export const adaptApiServiceToFrontend = (
    apiService?: Partial<ApiService>,
    businessInfo?: BusinessDetails
): FrontendService => {
    const name = apiService?.name ?? 'Service';
    const id = apiService?.id ?? slugify(name);
    const priceValue = typeof apiService?.price === 'number' ? apiService.price : 0;
    const durationValue = typeof apiService?.durationMinutes === 'number' ? apiService.durationMinutes : undefined;
    const tags = Array.isArray(apiService?.tags) && apiService.tags.length > 0
        ? apiService.tags
        : [slugify(name)];

    return {
        id,
        name,
        type: businessInfo?.name ?? apiService?.category ?? 'Service Provider',
        description: apiService?.description ?? (durationValue ? `${name} - ${durationValue} minutes` : `${name} service`),
        price: `R${Math.max(0, priceValue).toFixed(0)}`,
        rating: businessInfo?.rating ?? 4.5,
        reviewCount: businessInfo?.reviewCount ?? 0,
        location: businessInfo?.address ?? businessInfo?.city ?? 'Location TBD',
        availability: apiService?.isActive === false ? 'Unavailable' : 'Available',
        image: apiService?.imageUrl ?? '/placeholder.svg',
        phone: businessInfo?.phone ?? '+27 XX XXX XXXX',
        email: businessInfo?.email ?? 'contact@provider.co.za',
        tags,
        businessId: apiService?.businessId,
        providerName: apiService?.providerName ?? businessInfo?.providerName ?? businessInfo?.name ?? 'Service Provider'
    };
};

export const adaptServiceWithBusinessToFrontend = (
    serviceWithBusiness?: Partial<ServiceWithBusiness>
): FrontendService => {
    if (!serviceWithBusiness) {
        return adaptApiServiceToFrontend();
    }

    const { business, ...service } = serviceWithBusiness as Partial<ServiceWithBusiness> & Partial<ApiService> & { business?: ServiceWithBusiness['business'] };

    return adaptApiServiceToFrontend(service, business ? {
        name: business.businessName,
        city: business.city,
        address: business.address,
        phone: business.phone,
        email: business.email,
        rating: business.rating,
        reviewCount: business.reviewCount,
        providerName: business.providerName
    } : undefined);
};

export const adaptFrontendServiceToApi = (
    frontendService: Partial<FrontendService>,
    businessId: string
): {
    businessId: string;
    name: string;
    description?: string;
    category?: string;
    price: number;
    durationMinutes: number;
    imageUrl?: string;
    tags?: string[];
    isActive: boolean;
    providerName?: string;
} => {
    return {
        businessId,
        name: frontendService.name || '',
        description: frontendService.description,
        category: frontendService.type,
        price: parseFloat(frontendService.price?.replace('R', '') || '0'),
        durationMinutes: 60,
        imageUrl: frontendService.image !== '/placeholder.svg' ? frontendService.image : undefined,
        tags: frontendService.tags,
        isActive: frontendService.availability === 'Available',
        providerName: frontendService.providerName
    };
};

