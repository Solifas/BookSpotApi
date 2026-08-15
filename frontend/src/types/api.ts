// API Types matching the approved BookSpot contract

export type UserTypeValue = 'client' | 'provider';

export interface AuthResponse {
    accessToken: string;
    tokenType: 'Bearer';
    expiresAt: string;
    profile: Profile;
}

export interface RegisterRequest {
    email: string;
    fullName: string;
    contactNumber?: string | null;
    password: string;
    userType: UserTypeValue;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface Profile {
    profileId: string;
    email: string;
    fullName: string;
    contactNumber: string | null;
    userType: UserTypeValue;
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
    fullName?: string;
    contactNumber?: string | null;
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
    clientId?: string;
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
    description: string;
    address: string;
    phone: string;
    email: string;
    city: string;
    website?: string | null;
    imageUrl?: string | null;
    isActive?: boolean;
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
    code?: string;
    traceId?: string;
    errors?: Record<string, string[]>;
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
    service: Pick<Service, 'id' | 'businessId' | 'name' | 'price' | 'durationMinutes'> & Partial<Service>;
    client?: {
        id?: string;
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
    items: ServiceDto[];
    totalCount: number;
    page: number;
    pageSize: number;
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

export type BookingStatusValue = 'pending' | 'confirmed' | 'declined' | 'cancelled' | 'completed' | 'no_show';
export type BookingAction = 'confirm' | 'decline' | 'cancel' | 'complete' | 'mark_no_show' | 'reschedule';
export interface Money { amount: number; currency: 'ZAR'; }
export interface ServiceDto {
    serviceId: string; businessId: string; providerProfileId: string; providerDisplayName: string;
    name: string; description: string; category: string | null; price: Money; durationMinutes: number;
    imageUrl: string | null; tags: string[]; location: string | null; isActive: boolean; createdAt: string;
}
export interface AvailabilitySlotDto { startTime: string; endTime: string; }
export interface ServiceAvailabilityDto {
    serviceId: string; businessId: string; timeZone: string; from: string; to: string;
    durationMinutes: number; slots: AvailabilitySlotDto[];
}
export interface BookingActionRequest { action: BookingAction; expectedVersion: number; startTime?: string; }
export interface BookingMutationResultDto {
    view: 'client' | 'provider'; bookingId: string; status: BookingStatusValue;
    startTime: string; endTime: string; version: number; updatedAt: string;
}
export interface BookingDto {
    bookingId: string; serviceId: string; businessId: string; providerProfileId: string;
    status: BookingStatusValue; startTime: string; endTime: string; price: Money;
    version: number; createdAt: string; updatedAt: string;
    service: { name: string; durationMinutes: number };
    business: { businessName: string; address: string; city: string };
    view: 'client' | 'provider'; clientProfileId?: string;
    client?: { fullName: string; email: string; contactNumber: string | null };
}
export interface BookingPageDto { items: BookingDto[]; nextCursor: string | null; }
export interface BusinessDto {
    businessId: string; providerProfileId: string; businessName: string; description: string;
    address: string; city: string; phone: string; email: string; website: string | null;
    imageUrl: string | null; isActive: boolean; rating: number; reviewCount: number;
    timeZone: string; createdAt: string;
}
export interface UpdateBusinessRequest {
    businessName?: string; description?: string; address?: string; city?: string;
    phone?: string; email?: string; website?: string | null; imageUrl?: string | null; isActive?: boolean;
}
export interface ProviderDashboardDto {
    kind: 'provider'; generatedAt: string; timeZone: string; todayBookings: number;
    weekBookings: number; pendingRequests: number; totalClients: number; activeServices: number;
    monthlyRevenue: Money; upcoming: BookingDto[];
    recentClients: Array<{ clientProfileId: string; fullName: string; lastBookingAt: string; totalBookings: number }>;
}
export interface ClientDashboardDto {
    kind: 'client'; generatedAt: string; totalBookings: number; completedBookings: number;
    cancelledBookings: number; pendingRequests: number; totalSpent: Money;
    upcoming: BookingDto[]; recent: BookingDto[];
}
export type DashboardDto = ProviderDashboardDto | ClientDashboardDto;
