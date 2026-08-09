// API Types matching your OpenAPI specification

export interface AuthResponse {
    token: string;
    userId: string;
    email: string;
    fullName: string;
    contactNumber?: string;
    userType: string;
    expiresAt: string;
}

export interface RegisterRequest {
    email: string;
    fullName: string;
    contactNumber?: string;
    password: string;
    userType: string;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface Profile {
    id: string;
    email: string;
    fullName: string;
    contactNumber?: string;
    userType: string;
    passwordHash?: string;
    createdAt: string;
}

export interface Client {
    id: string;
    fullName: string;
    email: string;
    contactNumber?: string;
    totalBookings: number;
    lastVisit: string;
    avatarUrl?: string;
}

export interface UpdateProfileCommand {
    id: string;
    email?: string;
    userType?: string;
}

export interface CreateProfileCommand {
    email: string;
    userType: string;
}

export interface Service {
    id: string;
    businessId: string;
    name: string;
    description?: string;
    category?: string;
    price: number;
    durationMinutes: number;
    imageUrl?: string;
    tags?: string[];
    isActive: boolean;
    createdAt: string;
    providerName?: string;
}

export interface CreateServiceCommand {
    businessId: string;
    name: string;
    description?: string;
    category?: string;
    price: number;
    durationMinutes: number;
    imageUrl?: string;
    tags?: string[];
    isActive: boolean;
    location?: string;
}

export interface UpdateServiceCommand {
    id: string;
    name?: string;
    description?: string;
    category?: string;
    price?: number;
    durationMinutes?: number;
    imageUrl?: string;
    tags?: string[];
    isActive?: boolean;
    location?: string;
}

export interface Booking {
    id: string;
    serviceId: string;
    clientId: string;
    providerId: string;
    providerName?: string;
    startTime: string;
    endTime: string;
    status: string;
    createdAt: string;
}

export interface CreateBookingCommand {
    serviceId: string;
    startTime: string;
    endTime: string;
    providerName?: string;
}

export interface UpdateBookingCommand {
    id: string;
    startTime?: string;
    endTime?: string;
    status?: string;
    providerName?: string;
}

export interface Business {
    id: string;
    providerId: string;
    providerName?: string;
    businessName: string;
    description?: string;
    city: string;
    address?: string;
    phone?: string;
    email?: string;
    website?: string;
    imageUrl?: string;
    isActive: boolean;
    createdAt: string;
}

export interface CreateBusinessCommand {
    businessName: string;
    description?: string;
    address?: string;
    phone?: string;
    email?: string;
    city: string;
    website?: string;
    imageUrl?: string;
    isActive: boolean;
}

export interface UpdateBusinessCommand {
    id: string;
    businessName?: string;
    description?: string;
    address?: string;
    phone?: string;
    email?: string;
    city?: string;
    website?: string;
    imageUrl?: string;
    isActive?: boolean;
}

export interface BusinessHour {
    id: string;
    businessId: string;
    dayOfWeek: number;
    openTime: string;
    closeTime: string;
    isClosed: boolean;
}

export interface CreateBusinessHourCommand {
    businessId: string;
    dayOfWeek: number;
    openTime: string;
    closeTime: string;
    isClosed: boolean;
}

export interface UpdateBusinessHourCommand {
    id: string;
    dayOfWeek?: number;
    openTime?: string;
    closeTime?: string;
    isClosed?: boolean;
}

export interface Review {
    id: string;
    bookingId: string;
    rating: number;
    comment: string;
}

export interface CreateReviewCommand {
    bookingId: string;
    rating: number;
    comment: string;
}

export interface UpdateReviewCommand {
    id: string;
    rating?: number;
    comment?: string;
}

export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    instance?: string;
    [key: string]: any;
}

// Enhanced interfaces for better frontend integration
export interface ServiceWithBusiness extends Service {
    business: {
        id: string;
        businessName: string;
        city: string;
        address?: string;
        phone?: string;
        email?: string;
        rating?: number;
        reviewCount?: number;
        providerName?: string;
    };
}

export interface BookingWithDetails extends Booking {
    service: Service;
    client: {
        id: string;
        fullName: string;
        email: string;
        contactNumber?: string;
    };
    business: {
        id: string;
        businessName: string;
        city: string;
    };
}

// Search and filter interfaces
export interface ServiceSearchParams {
    name?: string;
    city?: string;
    category?: string;
    minPrice?: number;
    maxPrice?: number;
    minDuration?: number;
    maxDuration?: number;
    page?: number;
    pageSize?: number;
}

export interface ServiceSearchResponse {
    services: ServiceWithBusiness[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

// Booking status enum for better type safety
export enum BookingStatus {
    PENDING = 'pending',
    CONFIRMED = 'confirmed',
    COMPLETED = 'completed',
    CANCELLED = 'cancelled'
}

// User type enum for better type safety
export enum UserType {
    CLIENT = 'client',
    PROVIDER = 'provider'
}

// Dashboard statistics interface
export interface DashboardStats {
    todayBookings: number;
    weekBookings: number;
    totalClients: number;
    monthlyRevenue: number;
    pendingBookings: number;
    confirmedBookings: number;
}

// City information interface
export interface CityInfo {
    city: string;
    province: string;
    serviceCount: number;
}
