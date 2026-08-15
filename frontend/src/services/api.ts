import type {
  AuthResponse,
  BookingActionRequest,
  BookingDto,
  BookingMutationResultDto,
  BookingPageDto,
  Business,
  BusinessDto,
  CityInfo,
  Client,
  CreateBookingCommand,
  CreateBusinessCommand,

  CreateReviewCommand,
  CreateServiceCommand,
  DashboardDto,
  DashboardStats,
  Profile,
  RegisterRequest,
  Review,

  ServiceAvailabilityDto,
  ServiceDto,
  ServiceSearchParams,
  ServiceSearchResponse,
  UpdateBusinessCommand,

  UpdateBusinessRequest,

  UpdateProfileCommand,
  UpdateReviewCommand,
  UpdateServiceCommand,
} from '../types/api';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  status: number;
  code?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}


const randomBytes = (length: number): Uint8Array => {
  const bytes = new Uint8Array(length);
  const cryptoApi = globalThis.crypto;
  if (!cryptoApi?.getRandomValues) throw new Error('Secure random generation is unavailable.');
  cryptoApi.getRandomValues(bytes);
  return bytes;
};

const base64Url = (bytes: Uint8Array): string => {
  let binary = '';
  bytes.forEach((byte) => { binary += String.fromCharCode(byte); });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
};

export const createIdempotencyKey = (): string => {
  return base64Url(randomBytes(24));
};

export class ApiClient {
  private token: string | null = null;

  constructor(private readonly baseURL: string) {}

  setToken(token: string): void {
    this.token = token;
  }

  hasToken(): boolean { return this.token !== null; }

  clearToken(): void {
    this.token = null;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<ApiResponse<T>> {
    const headers = new Headers(options.headers);
    if (options.body != null && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
    if (this.token) headers.set('Authorization', `Bearer ${this.token}`);

    let response: Response;
    try {
      response = await fetch(`${this.baseURL}${endpoint}`, { ...options, headers });
    } catch (error) {
      return { status: 0, error: error instanceof Error ? error.message : 'Network request failed.' };
    }

    const text = await response.text();
    const contentType = response.headers.get('Content-Type')?.toLowerCase() ?? '';
    let parsed: unknown;
    let malformedJson = false;

    if (text) {
      if (contentType.includes('json')) {
        try {
          parsed = JSON.parse(text);
        } catch {
          malformedJson = true;
        }
      } else {
        parsed = text;
      }
    }

    if (!response.ok) {
      if (parsed && typeof parsed === 'object') {
        const problem = parsed as Record<string, unknown>;
        return {
          status: response.status,
          error: typeof problem.detail === 'string'
            ? problem.detail
            : typeof problem.title === 'string'
              ? problem.title
              : `Request failed with status ${response.status}`,
          code: typeof problem.code === 'string' ? problem.code : undefined,
          errors: problem.errors && typeof problem.errors === 'object'
            ? problem.errors as Record<string, string[]>
            : undefined,
          traceId: typeof problem.traceId === 'string' ? problem.traceId : undefined,
        };
      }
      return {
        status: response.status,
        error: typeof parsed === 'string' && !malformedJson && parsed.trim()
          ? parsed.trim()
          : `Request failed with status ${response.status}`,
      };
    }

    if (!text) return { status: response.status };
    if (malformedJson) return { status: response.status, error: 'Server returned an invalid JSON response.' };
    return { status: response.status, data: parsed as T };
  }

  login(email: string, password: string) {
    return this.request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) });
  }

  register(data: RegisterRequest) {
    return this.request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify(data) });
  }

  requestPasswordReset(email: string) {
    return this.request<{ message: string; success: true }>('/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email }) });
  }

  validateResetToken(token: string) {
    return this.request<{ valid: true }>('/auth/validate-reset-token', { method: 'POST', body: JSON.stringify({ token }) });
  }

  resetPassword(token: string, newPassword: string, idempotencyKey: string) {
    return this.request<{ message: string; success: true }>('/auth/reset-password', {
      method: 'POST',
      headers: { 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify({ token, newPassword }),
    });
  }

  getProfile() { return this.request<Profile>('/profiles/me'); }

  updateProfile(data: UpdateProfileCommand): Promise<ApiResponse<Profile>>;
  updateProfile(_legacyId: string, data: UpdateProfileCommand): Promise<ApiResponse<Profile>>;
  updateProfile(first: string | UpdateProfileCommand, second?: UpdateProfileCommand) {
    const data = typeof first === 'string' ? second ?? {} : first;
    return this.request<Profile>('/profiles/me', { method: 'PATCH', body: JSON.stringify(data) });
  }

  deleteMyProfile() { return this.request<void>('/profiles/me', { method: 'DELETE' }); }
  getMyBusinesses() { return this.request<BusinessDto[]>('/businesses/mine'); }
  getDashboard() { return this.request<DashboardDto>('/dashboard/me'); }

  getClientBookings(_legacyClientId?: string) { return this.request<BookingPageDto>('/bookings/client/me'); }

  getProviderBookings(
    _legacyProviderId?: string,
    status?: string,
    from?: string,
    to?: string,
  ) {
    const params = new URLSearchParams();
    if (status) params.set('status', status);
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    const query = params.toString();
    return this.request<BookingPageDto>(`/bookings/provider/me${query ? `?${query}` : ''}`);
  }

  createBooking(data: CreateBookingCommand, idempotencyKey: string) {
    return this.request<BookingMutationResultDto>('/bookings', {
      method: 'POST', headers: { 'Idempotency-Key': idempotencyKey }, body: JSON.stringify(data),
    });
  }

  getBooking(id: string) { return this.request<BookingDto>(`/bookings/${encodeURIComponent(id)}`); }

  performBookingAction(id: string, data: BookingActionRequest, idempotencyKey: string) {
    return this.request<BookingMutationResultDto>(`/bookings/${encodeURIComponent(id)}/actions`, {
      method: 'POST', headers: { 'Idempotency-Key': idempotencyKey }, body: JSON.stringify(data),
    });
  }

  async getServices(): Promise<ApiResponse<ServiceDto[]>> {
    const response = await this.searchServices({ page: 1, pageSize: 100 });
    return response.data
      ? { ...response, data: response.data.items }
      : { status: response.status, error: response.error, code: response.code, errors: response.errors, traceId: response.traceId };
  }
  getService(id: string) { return this.request<ServiceDto>(`/services/${encodeURIComponent(id)}`); }
  getServiceAvailability(id: string, from: string, to: string) {
    const params = new URLSearchParams({ from, to });
    return this.request<ServiceAvailabilityDto>(`/services/${encodeURIComponent(id)}/availability?${params}`);
  }
  searchServices(params: ServiceSearchParams) {
    const search = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) search.set(key === 'name' ? 'q' : key, String(value));
    });
    return this.request<ServiceSearchResponse>(`/services/search${search.size ? `?${search}` : ''}`);
  }
  createService(data: CreateServiceCommand) {
    const { price, ...fields } = data;
    return this.request<ServiceDto>('/services', { method: 'POST', body: JSON.stringify({ ...fields, priceAmount: price }) });
  }
  updateService(id: string, data: UpdateServiceCommand) {
    const { id: _ignored, price, ...fields } = data;
    void _ignored;
    return this.request<ServiceDto>(`/services/${encodeURIComponent(id)}`, {
      method: 'PATCH', body: JSON.stringify({ ...fields, ...(price === undefined ? {} : { priceAmount: price }) }),
    });
  }
  deleteService(id: string) { return this.request<void>(`/services/${encodeURIComponent(id)}`, { method: 'DELETE' }); }
  getBusinessServices(id: string) { return this.request<ServiceDto[]>(`/businesses/${encodeURIComponent(id)}/services`); }
  getBusinessServiceCatalog(id: string) { return this.request<ServiceDto[]>(`/businesses/${encodeURIComponent(id)}/services`); }

  createBusiness(data: CreateBusinessCommand) { return this.request<BusinessDto>('/businesses', { method: 'POST', body: JSON.stringify(data) }); }
  getBusiness(id: string) { return this.request<BusinessDto>(`/businesses/${encodeURIComponent(id)}`); }
  getBusinessDetails(id: string) { return this.request<BusinessDto>(`/businesses/${encodeURIComponent(id)}`); }
  updateBusiness(id: string, data: UpdateBusinessCommand | UpdateBusinessRequest) {
    const { id: _ignored, ...payload } = data as UpdateBusinessCommand;
    void _ignored;
    return this.request<BusinessDto>(`/businesses/${encodeURIComponent(id)}`, { method: 'PATCH', body: JSON.stringify(payload) });
  }


  createReview(data: CreateReviewCommand) { return this.request<Review>('/reviews', { method: 'POST', body: JSON.stringify(data) }); }
  updateReview(id: string, data: UpdateReviewCommand) {
    const { id: _ignored, ...payload } = data;
    void _ignored;
    return this.request<Review>(`/reviews/${encodeURIComponent(id)}`, { method: 'PATCH', body: JSON.stringify(payload) });
  }
  getCities() { return this.request<CityInfo[]>('/locations/cities'); }
  getClients(): Promise<ApiResponse<Client[]>> {
    return Promise.resolve({ status: 400, code: 'unsupported_client_directory', error: 'Use the party-scoped dashboard client summary.' });
  }

  // Compatibility wrappers for existing callers while they migrate to /dashboard/me.
  getProviderDashboardStats(_id?: string): Promise<ApiResponse<DashboardStats>> {
    return Promise.resolve({ status: 400, code: 'use_canonical_dashboard', error: 'Use the canonical dashboard response.' });
  }
  getClientDashboardStats(_id?: string): Promise<ApiResponse<DashboardStats>> {
    return Promise.resolve({ status: 400, code: 'use_canonical_dashboard', error: 'Use the canonical dashboard response.' });
  }
}

export const apiClient = new ApiClient(API_BASE_URL);
