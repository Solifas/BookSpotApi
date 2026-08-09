
export interface Service {
  id: string;
  name: string;
  type: string;
  description: string;
  price: string;
  rating: number;
  reviewCount: number;
  location: string;
  availability: string;
  image: string;
  phone: string;
  email: string;
  tags: string[];
  businessId?: string;
  providerId?: string;
  providerName?: string;
}

export interface ServiceProvider {
  id: string;
  name: string;
  businessName: string;
  services: Service[];
  location: string;
  rating: number;
  reviewCount: number;
  profileImage: string;
  verified: boolean;
}

export const mockServices: Service[] = [
  {
    id: '1',
    name: 'Premium Men\'s Haircut',
    type: 'Barber',
    description: 'Professional men\'s haircut with styling and beard trim',
    price: 'R150',
    rating: 4.8,
    reviewCount: 127,
    location: 'Sandton City, Johannesburg',
    availability: 'Available today',
    image: '/placeholder.svg',
    phone: '+27 11 123 4567',
    email: 'contact@premiumcuts.co.za',
    tags: ['men', 'haircut', 'beard', 'styling'],
    businessId: 'business-1',
    providerId: 'provider-1'
  },
  {
    id: '2',
    name: 'Ladies Hair Styling',
    type: 'Hair Salon',
    description: 'Complete hair styling service including wash, cut, and blow-dry',
    price: 'R280',
    rating: 4.9,
    reviewCount: 89,
    location: 'Rosebank, Johannesburg',
    availability: 'Next available: Tomorrow',
    image: '/placeholder.svg',
    phone: '+27 11 234 5678',
    email: 'bookings@glamoursalon.co.za',
    tags: ['women', 'styling', 'wash', 'cut'],
    businessId: 'business-2',
    providerId: 'provider-2'
  },
  {
    id: '3',
    name: 'Relaxing Facial Treatment',
    type: 'Beauty Therapist',
    description: 'Deep cleansing facial with moisturizing treatment',
    price: 'R320',
    rating: 4.7,
    reviewCount: 156,
    location: 'Century City, Cape Town',
    availability: 'Available this week',
    image: '/placeholder.svg',
    phone: '+27 21 345 6789',
    email: 'spa@beautybliss.co.za',
    tags: ['facial', 'skincare', 'relaxing', 'cleansing'],
    businessId: 'business-3',
    providerId: 'provider-3'
  },
  {
    id: '4',
    name: 'Deep Tissue Massage',
    type: 'Massage Therapist',
    description: 'Therapeutic deep tissue massage for muscle tension relief',
    price: 'R450',
    rating: 4.9,
    reviewCount: 203,
    location: 'Durban North, KZN',
    availability: 'Available today',
    image: '/placeholder.svg',
    phone: '+27 31 456 7890',
    email: 'therapy@massageheaven.co.za',
    tags: ['massage', 'therapeutic', 'muscle', 'relaxation'],
    businessId: 'business-4',
    providerId: 'provider-4'
  },
  {
    id: '5',
    name: 'Personal Training Session',
    type: 'Personal Trainer',
    description: 'One-on-one fitness training session with customized workout plan',
    price: 'R400',
    rating: 4.6,
    reviewCount: 94,
    location: 'Hatfield, Pretoria',
    availability: 'Available this week',
    image: '/placeholder.svg',
    phone: '+27 12 567 8901',
    email: 'fit@strongbody.co.za',
    tags: ['fitness', 'training', 'workout', 'health'],
    businessId: 'business-5',
    providerId: 'provider-5'
  },
  {
    id: '6',
    name: 'Math Tutoring',
    type: 'Tutor',
    description: 'High school mathematics tutoring for grades 10-12',
    price: 'R250/hour',
    rating: 4.8,
    reviewCount: 67,
    location: 'Stellenbosch, Western Cape',
    availability: 'Available weekends',
    image: '/placeholder.svg',
    phone: '+27 21 678 9012',
    email: 'learn@mathgenius.co.za',
    tags: ['education', 'math', 'tutoring', 'high school'],
    businessId: 'business-6',
    providerId: 'provider-6'
  },
  {
    id: '7',
    name: 'Emergency Plumbing',
    type: 'Plumber',
    description: '24/7 emergency plumbing services for residential and commercial',
    price: 'R350 call-out',
    rating: 4.5,
    reviewCount: 189,
    location: 'Port Elizabeth, Eastern Cape',
    availability: '24/7 Emergency',
    image: '/placeholder.svg',
    phone: '+27 41 789 0123',
    email: 'emergency@quickfix.co.za',
    tags: ['plumbing', 'emergency', '24/7', 'repair'],
    businessId: 'business-7',
    providerId: 'provider-7'
  },
  {
    id: '8',
    name: 'Electrical Installation',
    type: 'Electrician',
    description: 'Professional electrical installation and maintenance services',
    price: 'R300/hour',
    rating: 4.7,
    reviewCount: 112,
    location: 'Bloemfontein, Free State',
    availability: 'Available weekdays',
    image: '/placeholder.svg',
    phone: '+27 51 890 1234',
    email: 'power@sparkelectric.co.za',
    tags: ['electrical', 'installation', 'maintenance', 'professional'],
    businessId: 'business-8',
    providerId: 'provider-8'
  }
];

export const getServicesByType = (type: string): Service[] => {
  if (!type) return mockServices;
  return mockServices.filter(service =>
    service.type.toLowerCase().includes(type.toLowerCase())
  );
};

export const getServicesByLocation = (location: string): Service[] => {
  if (!location) return mockServices;
  return mockServices.filter(service =>
    service.location.toLowerCase().includes(location.toLowerCase())
  );
};

export const searchServices = (query: string, location: string): Service[] => {
  let filteredServices = mockServices;

  if (query) {
    filteredServices = filteredServices.filter(service =>
      service.type.toLowerCase().includes(query.toLowerCase()) ||
      service.name.toLowerCase().includes(query.toLowerCase()) ||
      service.description.toLowerCase().includes(query.toLowerCase()) ||
      service.tags.some(tag => tag.toLowerCase().includes(query.toLowerCase()))
    );
  }

  if (location) {
    filteredServices = filteredServices.filter(service =>
      service.location.toLowerCase().includes(location.toLowerCase())
    );
  }

  return filteredServices;
};
