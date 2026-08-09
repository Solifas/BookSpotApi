
import { useState, useEffect } from 'react';
import { Calendar, Clock, MapPin, User, Star, LogIn, Loader2, ArrowLeft, Phone, Mail } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import AuthModal from '../components/AuthModal';
import { DataSourceAdapter } from '../services/dataSourceAdapter';
import { Service } from '../types/api';
import { isMockMode } from '../config/dataSource';
import { Badge } from '@/components/ui/badge';
import { useCreateBooking } from '../hooks/useBookings';
import { toast } from '@/hooks/use-toast';
import { useNavigate, useParams } from 'react-router-dom';

interface LocalService {
  id: string;
  name: string;
  duration: string;
  price: string;
  description: string;
  businessId?: string;
  type?: string;
  providerName?: string;
}

interface ProviderContact {
  email?: string;
  phone?: string;
}

const BookingPage = () => {
  const { user, isLoggedIn } = useAuth();
  const navigate = useNavigate();
  const { providerId } = useParams<{ providerId: string }>();
  const [selectedService, setSelectedService] = useState('');
  const [selectedDate, setSelectedDate] = useState('');
  const [selectedTime, setSelectedTime] = useState('');
  const [step, setStep] = useState(1);
  const [showAuth, setShowAuth] = useState(false);
  const [authMode, setAuthMode] = useState<'login' | 'signup'>('login');
  const [services, setServices] = useState<LocalService[]>([]);
  const [servicesLoading, setServicesLoading] = useState(true);
  const [businessName, setBusinessName] = useState('Service Provider');
  const [providerContact, setProviderContact] = useState<ProviderContact>({});
  const selectedServiceProviderName = services.find(s => s.id === selectedService)?.providerName;

  const createBookingMutation = useCreateBooking();

  // Mock services - only used in mock mode
  const mockServices: LocalService[] = [
    { id: 'f91b7cd3-c51d-4898-ad4d-af71770b23ee', name: 'Classic Haircut', duration: '30 min', price: 'R120', description: 'Professional cut and styling', businessId: 'business-1', type: 'Barber' },
    { id: 'f91b7cd3-c51d-4898-ad4d-af71770b23ea', name: 'Beard Trim & Shape', duration: '20 min', price: 'R80', description: 'Expert beard grooming', businessId: 'business-1', type: 'Barber' },
    { id: 'f91b7cd3-c51d-4898-ad4d-af71770b23eb', name: 'Cut & Beard Combo', duration: '45 min', price: 'R180', description: 'Complete grooming package', businessId: 'business-1', type: 'Barber' },
    { id: 'f91b7cd3-c51d-4898-ad4d-af71770b23ec', name: 'Hair Styling', duration: '25 min', price: 'R100', description: 'Professional styling service', businessId: 'business-1', type: 'Barber' }
  ];

  // Load services on component mount
  useEffect(() => {
    const loadServices = async () => {
      setServicesLoading(true);

      if (!providerId) {
        console.error('No provider ID provided');
        setServicesLoading(false);
        return;
      }

      try {
        if (isMockMode()) {
          console.log(`🎭 BookingPage: Using mock services for provider ${providerId}`);
          // Filter mock services by businessId
          const filteredServices = mockServices.filter(s => s.businessId === providerId);
          setServices(filteredServices);
          setBusinessName(filteredServices[0]?.providerName || 'Service Provider');
        } else {
          console.log(`🌐 BookingPage: Fetching services for provider ${providerId}`);

          // Use public search endpoint to get all services, then filter by businessId
          const response = await DataSourceAdapter.searchServices({ page: 1, pageSize: 100 });

          if (response.error) {
            throw new Error(response.error);
          }

          // Filter services by businessId (response.data is an array of services)
          const allServices = Array.isArray(response.data) ? response.data : [];
          const businessServices = allServices.filter(
            (service: Service) => service.businessId === providerId
          );

          // Try to get business name from first service or fetch business details
          if (businessServices.length > 0) {
            const businessResponse = await DataSourceAdapter.getBusiness(providerId);
            if (businessResponse.data) {
              setBusinessName(businessResponse.data.address);
              setProviderContact({
                email: businessResponse.data.email,
                phone: businessResponse.data.phone
              });
            }
          }

          // Convert API services to local format
          const apiServices: LocalService[] = businessServices.map((service: Service) => ({
            id: service.id,
            name: service.name,
            duration: `${service.durationMinutes} min`,
            price: `R${service.price}`,
            description: service.description || 'Professional service',
            providerName: service.providerName
          }));

          setServices(apiServices);
        }
      } catch (err) {
        console.error('Failed to load services:', err);
        // Fallback to mock data in mock mode, empty array in API mode
        if (isMockMode()) {
          const filteredServices = mockServices.filter(s => s.businessId === providerId);
          setServices(filteredServices);
        } else {
          setServices([]);
        }
      } finally {
        setServicesLoading(false);
      }
    };

    loadServices();
  }, [providerId]);

  const timeSlots = [
    '08:00', '08:30', '09:00', '09:30', '10:00', '10:30',
    '11:00', '11:30', '12:00', '12:30', '14:00', '14:30',
    '15:00', '15:30', '16:00', '16:30', '17:00', '17:30'
  ];

  const handleConfirmBooking = async () => {
    if (!user || !selectedService || !selectedDate || !selectedTime) {
      toast({
        title: "Missing information",
        description: "Please ensure all booking details are filled in.",
        variant: "destructive",
      });
      return;
    }

    try {
      await createBookingMutation.mutateAsync({
        serviceId: selectedService,
        date: new Date(selectedDate),
        timeSlot: selectedTime,
        clientName: user.name || '',
        clientPhone: user.contactNumber || '',
        clientEmail: user.email || '',
        providerName: selectedServiceProviderName || '',
      });

      toast({
        title: "Booking submitted!",
        description: "Your booking has been submitted and is pending confirmation from the provider.",
      });

      // Navigate to dashboard or home after short delay
      setTimeout(() => {
        navigate('/');
      }, 2000);
    } catch (error) {
      toast({
        title: "Booking failed",
        description: `There was an error creating your booking. ${error.message} `,
        variant: "destructive",
      });
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
      {/* Header */}
      <div className="bg-white shadow-sm">
        <div className="max-w-4xl mx-auto px-3 sm:px-4 lg:px-6 py-4 sm:py-6">
          {/* Back Button */}
          <button
            onClick={() => navigate('/')}
            className="flex items-center gap-2 text-slate-600 hover:text-slate-900 mb-4 transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            <span className="text-sm font-medium">Back to Home</span>
          </button>

          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div className="flex items-center space-x-3 sm:space-x-4 w-full sm:w-auto">
              <div className="w-12 h-12 sm:w-16 sm:h-16 bg-gradient-to-br from-blue-400 to-green-400 rounded-xl sm:rounded-2xl flex items-center justify-center flex-shrink-0">
                <User className="h-6 w-6 sm:h-8 sm:w-8 text-white" />
              </div>
              <div className="min-w-0 flex-1">
                <h1 className="text-xl sm:text-2xl font-bold text-slate-900 truncate">{services[0]?.providerName}</h1>
                <div className="flex flex-col sm:flex-row sm:items-center sm:space-x-4 text-slate-600 mt-1 gap-1 sm:gap-0">
                  <div className="flex items-center space-x-1">
                    <MapPin className="h-3 w-3 sm:h-4 sm:w-4 flex-shrink-0" />
                    <span className="text-xs sm:text-sm truncate">I need to add location</span>
                  </div>
                  <div className="flex items-center space-x-1">
                    <Star className="h-3 w-3 sm:h-4 sm:w-4 fill-yellow-400 text-yellow-400 flex-shrink-0" />
                    <span className="font-medium text-xs sm:text-sm">Add an api to get the rating</span>
                  </div>
                </div>
              </div>
            </div>

            {!isLoggedIn && (
              <button
                onClick={() => {
                  setAuthMode('login');
                  setShowAuth(true);
                }}
                className="w-full sm:w-auto flex items-center justify-center space-x-2 bg-gradient-to-r from-blue-500 to-green-500 text-white px-4 sm:px-6 py-2 sm:py-2 rounded-full font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl text-sm"
              >
                <LogIn className="h-4 w-4" />
                <span>Login</span>
              </button>
            )}

            {isLoggedIn && (
              <div className="flex items-center space-x-2 text-slate-700 text-sm">
                <User className="h-4 w-4" />
                <span className="font-medium">Welcome, {user?.name}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-3 sm:px-4 lg:px-6 py-6 sm:py-8">
        {/* Progress Steps */}
        <div className="flex items-center justify-center mb-6 sm:mb-8 overflow-x-auto">
          <div className="flex items-center space-x-2 sm:space-x-4 min-w-max px-4">
            <div className={`w-6 h-6 sm:w-8 sm:h-8 rounded-full flex items-center justify-center font-medium text-sm ${step >= 1 ? 'bg-blue-500 text-white' : 'bg-slate-200 text-slate-600'}`}>
              1
            </div>
            <div className={`w-8 sm:w-16 h-1 ${step >= 2 ? 'bg-blue-500' : 'bg-slate-200'}`}></div>
            <div className={`w-6 h-6 sm:w-8 sm:h-8 rounded-full flex items-center justify-center font-medium text-sm ${step >= 2 ? 'bg-blue-500 text-white' : 'bg-slate-200 text-slate-600'}`}>
              2
            </div>
            <div className={`w-8 sm:w-16 h-1 ${step >= 3 ? 'bg-blue-500' : 'bg-slate-200'}`}></div>
            <div className={`w-6 h-6 sm:w-8 sm:h-8 rounded-full flex items-center justify-center font-medium text-sm ${step >= 3 ? 'bg-blue-500 text-white' : 'bg-slate-200 text-slate-600'}`}>
              3
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6 md:p-8">
          {step === 1 && (
            <div>
              <div className="flex items-center gap-3 mb-4 sm:mb-6">
                <h2 className="text-xl sm:text-2xl font-bold text-slate-900">Choose Your Service</h2>
                {isMockMode() && (
                  <Badge variant="outline">Mock Mode</Badge>
                )}
              </div>

              {servicesLoading ? (
                <div className="flex items-center justify-center py-12">
                  <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
                  <span className="ml-2 text-slate-600">Loading services...</span>
                </div>
              ) : services.length > 0 ? (
                <div className="grid grid-cols-1 gap-3 sm:gap-4">
                  {services.map((service) => (
                    <div
                      key={service.id}
                      onClick={() => setSelectedService(service.id)}
                      className={`p-4 sm:p-6 rounded-xl sm:rounded-2xl border-2 cursor-pointer transition-all duration-200 hover:shadow-lg ${selectedService === service.id
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-slate-200 hover:border-blue-300'
                        }`}
                    >
                      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2 mb-2">
                        <h3 className="text-base sm:text-lg font-semibold text-slate-900">{service.name}</h3>
                        <span className="text-lg font-bold text-blue-600">{service.price}</span>
                      </div>
                      <p className="text-slate-600 text-sm mb-2">{service.description}</p>
                      <div className="flex items-center space-x-1 text-slate-500 text-sm">
                        <Clock className="h-4 w-4" />
                        <span>{service.duration}</span>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-12">
                  <p className="text-slate-600 mb-4">
                    {isMockMode()
                      ? "No services available in mock mode"
                      : "No services available. Please check back later."
                    }
                  </p>
                </div>
              )}

              {selectedService && services.length > 0 && (
                <div className="mt-6 sm:mt-8 flex justify-end">
                  <button
                    onClick={() => setStep(2)}
                    className="w-full sm:w-auto bg-gradient-to-r from-blue-500 to-green-500 text-white px-6 sm:px-8 py-3 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl"
                  >
                    Continue to Date & Time
                  </button>
                </div>
              )}
            </div>
          )}

          {step === 2 && (
            <div>
              <h2 className="text-xl sm:text-2xl font-bold text-slate-900 mb-4 sm:mb-6">Select Date & Time</h2>

              {/* Date Selection */}
              <div className="mb-6 sm:mb-8">
                <h3 className="text-base sm:text-lg font-semibold text-slate-900 mb-3 sm:mb-4">Choose Date</h3>
                <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-7 gap-2 sm:gap-3">
                  {Array.from({ length: 14 }, (_, i) => {
                    const date = new Date();
                    date.setDate(date.getDate() + i);
                    const dateStr = date.toISOString().split('T')[0];
                    const dayName = date.toLocaleDateString('en-US', { weekday: 'short' });
                    const dayNum = date.getDate();

                    return (
                      <div
                        key={dateStr}
                        onClick={() => setSelectedDate(dateStr)}
                        className={`p-3 sm:p-4 rounded-lg sm:rounded-xl border-2 cursor-pointer transition-all duration-200 text-center ${selectedDate === dateStr
                          ? 'border-blue-500 bg-blue-50'
                          : 'border-slate-200 hover:border-blue-300'
                          }`}
                      >
                        <div className="text-xs sm:text-sm text-slate-600">{dayName}</div>
                        <div className="text-sm sm:text-lg font-semibold text-slate-900">{dayNum}</div>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Time Selection */}
              {selectedDate && (
                <div className="mb-6 sm:mb-8">
                  <h3 className="text-base sm:text-lg font-semibold text-slate-900 mb-3 sm:mb-4">Choose Time</h3>
                  <div className="grid grid-cols-3 sm:grid-cols-4 lg:grid-cols-6 gap-2 sm:gap-3">
                    {timeSlots.map((time) => (
                      <button
                        key={time}
                        onClick={() => setSelectedTime(time)}
                        className={`p-2 sm:p-3 rounded-lg sm:rounded-xl border-2 font-medium transition-all duration-200 text-sm ${selectedTime === time
                          ? 'border-blue-500 bg-blue-50 text-blue-700'
                          : 'border-slate-200 text-slate-700 hover:border-blue-300'
                          }`}
                      >
                        {time}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              <div className="flex flex-col sm:flex-row justify-between gap-3">
                <button
                  onClick={() => setStep(1)}
                  className="w-full sm:w-auto px-6 sm:px-8 py-3 rounded-xl font-medium text-slate-700 border-2 border-slate-200 hover:bg-slate-50 transition-all duration-200"
                >
                  Back
                </button>
                {selectedDate && selectedTime && (
                  <button
                    onClick={() => isLoggedIn ? setStep(3) : setShowAuth(true)}
                    className="w-full sm:w-auto bg-gradient-to-r from-blue-500 to-green-500 text-white px-6 sm:px-8 py-3 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl"
                  >
                    {isLoggedIn ? 'Review Booking' : 'Login to Continue'}
                  </button>
                )}
              </div>
            </div>
          )}

          {step === 3 && isLoggedIn && (
            <div>
              <h2 className="text-xl sm:text-2xl font-bold text-slate-900 mb-4 sm:mb-6">Confirm Booking</h2>

              {/* Booking Summary */}
              <div className="bg-slate-50 rounded-xl sm:rounded-2xl p-4 sm:p-6 mb-6 sm:mb-8">
                <h3 className="text-base sm:text-lg font-semibold text-slate-900 mb-3 sm:mb-4">Booking Summary</h3>
                <div className="space-y-2 sm:space-y-3">
                  <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                    <span className="text-slate-600 text-sm">Service:</span>
                    <span className="font-medium text-slate-900 text-sm">
                      {services.find(s => s.id === selectedService)?.name}
                    </span>
                  </div>
                  <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                    <span className="text-slate-600 text-sm">Date:</span>
                    <span className="font-medium text-slate-900 text-sm">
                      {new Date(selectedDate).toLocaleDateString('en-ZA')}
                    </span>
                  </div>
                  <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                    <span className="text-slate-600 text-sm">Time:</span>
                    <span className="font-medium text-slate-900 text-sm">{selectedTime}</span>
                  </div>
                  <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                    <span className="text-slate-600 text-sm">Duration:</span>
                    <span className="font-medium text-slate-900 text-sm">
                      {services.find(s => s.id === selectedService)?.duration}
                    </span>
                  </div>
                  <div className="border-t border-slate-200 pt-2 sm:pt-3 flex flex-col sm:flex-row sm:justify-between gap-1">
                    <span className="font-semibold text-slate-900 text-sm">Total:</span>
                    <span className="font-bold text-base sm:text-lg text-blue-600">
                      {services.find(s => s.id === selectedService)?.price}
                    </span>
                  </div>
                  {selectedServiceProviderName && (
                    <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                      <span className="text-slate-600 text-sm">Provider:</span>
                      <span className="font-medium text-slate-900 text-sm">
                        {selectedServiceProviderName}
                      </span>
                    </div>
                  )}
                </div>
              </div>

              {/* Provider Contact Information */}
              {(providerContact.email || providerContact.phone) && (
                <div className="bg-blue-50 rounded-xl sm:rounded-2xl p-4 sm:p-6 mb-6 sm:mb-8 border border-blue-100">
                  <h3 className="text-base sm:text-lg font-semibold text-slate-900 mb-3 sm:mb-4">Provider Contact</h3>
                  <div className="space-y-2">
                    {providerContact.phone && (
                      <div className="flex items-center gap-2 text-sm">
                        <Phone className="h-4 w-4 text-blue-600" />
                        <a href={`tel:${providerContact.phone}`} className="text-slate-700 hover:text-blue-600 transition-colors">
                          {providerContact.phone}
                        </a>
                      </div>
                    )}
                    {providerContact.email && (
                      <div className="flex items-center gap-2 text-sm">
                        <Mail className="h-4 w-4 text-blue-600" />
                        <a href={`mailto:${providerContact.email}`} className="text-slate-700 hover:text-blue-600 transition-colors">
                          {providerContact.email}
                        </a>
                      </div>
                    )}
                  </div>
                </div>
              )}

              <div className="flex flex-col sm:flex-row justify-between gap-3">
                <button
                  onClick={() => setStep(2)}
                  className="w-full sm:w-auto px-6 sm:px-8 py-3 rounded-xl font-medium text-slate-700 border-2 border-slate-200 hover:bg-slate-50 transition-all duration-200"
                >
                  Back
                </button>
                <button
                  onClick={handleConfirmBooking}
                  disabled={createBookingMutation.isPending}
                  className="w-full sm:w-auto bg-gradient-to-r from-blue-500 to-green-500 text-white px-6 sm:px-8 py-3 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                >
                  {createBookingMutation.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
                  {createBookingMutation.isPending ? 'Submitting...' : 'Confirm Booking'}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      <AuthModal
        isOpen={showAuth}
        onClose={() => setShowAuth(false)}
        mode={authMode}
        onModeChange={setAuthMode}
        defaultUserType="client"
      />
    </div>
  );
};

export default BookingPage;
