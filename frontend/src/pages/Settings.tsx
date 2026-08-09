
import { useState, useEffect } from 'react';
import { Clock, MapPin, DollarSign, User, Bell, Calendar, Loader2 } from 'lucide-react';
import Navigation from '../components/Navigation';
import { DataSourceAdapter } from '../services/dataSourceAdapter';
import { Service, Business } from '../types/api';
import { isMockMode } from '../config/dataSource';
import { Badge } from '@/components/ui/badge';
import { useAuth } from '../contexts/AuthContext';
import { toast } from 'sonner';

interface LocalService {
  name: string;
  duration: string;
  price: string;
}

const Settings = () => {
  const { user, isLoggedIn } = useAuth();
  const [activeTab, setActiveTab] = useState(user?.type === 'client' ? 'profile' : 'business');
  const [services, setServices] = useState<LocalService[]>([]);
  const [servicesLoading, setServicesLoading] = useState(true);
  const [businessData, setBusinessData] = useState<Business | null>(null);
  const [businessLoading, setBusinessLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  // Form state for business/location
  const [formData, setFormData] = useState({
    businessName: '',
    ownerName: '',
    phone: '',
    email: '',
    description: '',
    streetAddress: '',
    city: '',
    province: '',
    postalCode: '',
    googleMapsLink: ''
  });

  // Mock data - only used in mock mode
  const mockServices: LocalService[] = [
    { name: 'Classic Haircut', duration: '30', price: '120' },
    { name: 'Beard Trim & Shape', duration: '20', price: '80' },
    { name: 'Cut & Beard Combo', duration: '45', price: '180' },
    { name: 'Hair Styling', duration: '25', price: '100' }
  ];

  const mockBusinessData = {
    businessName: "Jabu's Barbershop",
    ownerName: 'Jabu Sithole',
    phone: '+27 82 123 4567',
    email: 'jabu@barbershop.co.za',
    description: 'Professional barbershop specializing in modern cuts, traditional shaves, and beard grooming. Serving the Sandton community for over 5 years.',
    streetAddress: 'Shop 12, Sandton City Mall',
    city: 'Sandton',
    province: 'Gauteng',
    postalCode: '2196',
    googleMapsLink: ''
  };

  // Load business data
  useEffect(() => {
    if (user?.type === 'provider' && isLoggedIn) {
      const loadBusinessData = async () => {
        setBusinessLoading(true);
        try {
          if (isMockMode()) {
            console.log('🎭 Settings: Using mock business data');
            setFormData(mockBusinessData);
          } else {
            console.log('🌐 Settings: Fetching business data for user:', user.id);
            const response = await DataSourceAdapter.getBusiness(user.id);

            if (response.error) {
              throw new Error(response.error);
            }

            if (response.data) {
              setBusinessData(response.data);
              setFormData({
                businessName: response.data.businessName,
                ownerName: user.name,
                phone: response.data.phone || '',
                email: response.data.email || '',
                description: response.data.description || '',
                streetAddress: response.data.address || '',
                city: response.data.city || '',
                province: '',
                postalCode: '',
                googleMapsLink: ''
              });
            }
          }
        } catch (err) {
          console.error('Failed to load business data:', err);
          if (isMockMode()) {
            setFormData(mockBusinessData);
          }
        } finally {
          setBusinessLoading(false);
        }
      };

      loadBusinessData();
    }
  }, [user, isLoggedIn]);

  // Load services when services tab is active
  useEffect(() => {
    if (activeTab === 'services') {
      const loadServices = async () => {
        setServicesLoading(true);

        try {
          if (isMockMode()) {
            console.log('🎭 Settings: Using mock services');
            setServices(mockServices);
          } else {
            if (!isLoggedIn || !user) {
              throw new Error('User must be logged in to access services');
            }

            console.log('🌐 Settings: Fetching services from API for user:', user.id);
            const response = await DataSourceAdapter.getBusinessServices(user.id);

            if (response.error) {
              throw new Error(response.error);
            }

            // Convert API services to local format
            const apiServices: LocalService[] = (response.data || []).map((service: Service) => ({
              name: service.name,
              duration: service.durationMinutes.toString(),
              price: service.price.toString()
            }));

            setServices(apiServices);
          }
        } catch (err) {
          console.error('Failed to load services:', err);
          // Fallback to mock data in mock mode, empty array in API mode
          if (isMockMode()) {
            setServices(mockServices);
          } else {
            setServices([]);
          }
        } finally {
          setServicesLoading(false);
        }
      };

      loadServices();
    }
  }, [activeTab]);

  // Handle form field changes
  const handleInputChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  // Handle save changes
  const handleSaveChanges = async () => {
    if (!user || user.type !== 'provider') return;

    // Validate required fields
    if (!formData.streetAddress || formData.streetAddress.trim().length < 5 || formData.streetAddress.trim().length > 200) {
      toast.error('Business address is required and must be between 5 and 200 characters');
      return;
    }

    if (!formData.city || formData.city.trim().length < 2 || formData.city.trim().length > 50) {
      toast.error('City is required and must be between 2 and 50 characters');
      return;
    }

    // Validate city contains only valid characters (letters, spaces, hyphens)
    const cityRegex = /^[a-zA-Z\s\-]+$/;
    if (!cityRegex.test(formData.city.trim())) {
      toast.error('City contains invalid characters. Use only letters, spaces, and hyphens');
      return;
    }

    setIsSaving(true);
    try {
      if (isMockMode()) {
        console.log('🎭 Settings: Mock mode - simulating save');
        await new Promise(resolve => setTimeout(resolve, 500));
        toast.success('Settings saved successfully (Mock mode)');
      } else {
        console.log('🌐 Settings: Saving business data via API');

        // Update business info
        const updateData = {
          businessName: formData.businessName,
          phone: formData.phone,
          email: formData.email,
          description: formData.description,
          address: formData.streetAddress.trim(),
          city: formData.city.trim(),
          id: user.id
        };

        const response = await DataSourceAdapter.updateBusiness(user.id, updateData);

        if (response.error) {
          throw new Error(response.error);
        }

        toast.success('Settings saved successfully');
      }
    } catch (err) {
      console.error('Failed to save settings:', err);
      toast.error('Failed to save settings. Please try again.');
    } finally {
      setIsSaving(false);
    }
  };

  const tabs = user?.type === 'client' ? [
    { id: 'profile', label: 'Profile', icon: User },
    { id: 'notifications', label: 'Notifications', icon: Bell }
  ] : [
    { id: 'business', label: 'Business Info', icon: User },
    { id: 'services', label: 'Services', icon: DollarSign },
    { id: 'availability', label: 'Availability', icon: Clock },
    { id: 'location', label: 'Location', icon: MapPin },
    { id: 'notifications', label: 'Notifications', icon: Bell }
  ];

  const weekDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      <Navigation />

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-slate-900 mb-2">{user?.type === 'client' ? 'Account Settings' : 'Business Settings'}</h1>
          <p className="text-slate-600">{user?.type === 'client' ? 'Manage your account information and preferences.' : 'Manage your business information and preferences.'}</p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Sidebar */}
          <div className="lg:col-span-1">
            <nav className="space-y-2">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`w-full flex items-center space-x-3 px-4 py-3 rounded-xl text-left transition-all duration-200 ${activeTab === tab.id
                    ? 'bg-blue-50 text-blue-700 border-l-4 border-blue-500'
                    : 'text-slate-700 hover:bg-slate-100'
                    }`}
                >
                  <tab.icon className="h-5 w-5" />
                  <span className="font-medium">{tab.label}</span>
                </button>
              ))}
            </nav>
          </div>

          {/* Content */}
          <div className="lg:col-span-3">
            <div className="bg-white rounded-2xl shadow-lg border border-slate-100 p-6 md:p-8">

              {(activeTab === 'business' || activeTab === 'profile') && (
                <div>
                  <div className="flex items-center gap-3 mb-6">
                    <h2 className="text-2xl font-bold text-slate-900">{user?.type === 'client' ? 'Profile Information' : 'Business Information'}</h2>
                    {isMockMode() && (
                      <Badge variant="outline">Mock Mode</Badge>
                    )}
                  </div>

                  {businessLoading ? (
                    <div className="flex items-center justify-center py-12">
                      <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
                      <span className="ml-2 text-slate-600">Loading business information...</span>
                    </div>
                  ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      {user?.type === 'provider' && (
                        <div>
                          <label className="block text-sm font-medium text-slate-700 mb-2">Business Name</label>
                          <input
                            type="text"
                            className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                            value={formData.businessName}
                            onChange={(e) => handleInputChange('businessName', e.target.value)}
                          />
                        </div>
                      )}
                      <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">{user?.type === 'client' ? 'Full Name' : 'Owner Name'}</label>
                        <input
                          type="text"
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={user?.type === 'client' ? user?.name || '' : formData.ownerName}
                          onChange={(e) => handleInputChange('ownerName', e.target.value)}
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">Phone Number</label>
                        <input
                          type="tel"
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={user?.contactNumber || formData.phone}
                          onChange={(e) => handleInputChange('phone', e.target.value)}
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">Email Address</label>
                        <input
                          type="email"
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={user.email || formData.email}
                          onChange={(e) => handleInputChange('email', e.target.value)}
                        />
                      </div>
                      {user?.type === 'provider' && (
                        <div className="md:col-span-2">
                          <label className="block text-sm font-medium text-slate-700 mb-2">Business Description</label>
                          <textarea
                            rows={4}
                            className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                            value={formData.description}
                            onChange={(e) => handleInputChange('description', e.target.value)}
                          />
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'services' && (
                <div>
                  <div className="flex justify-between items-center mb-6">
                    <div className="flex items-center gap-3">
                      <h2 className="text-2xl font-bold text-slate-900">Services & Pricing</h2>
                      {isMockMode() && (
                        <Badge variant="outline">Mock Mode</Badge>
                      )}
                    </div>
                    <button className="bg-gradient-to-r from-blue-500 to-green-500 text-white px-4 py-2 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200">
                      + Add Service
                    </button>
                  </div>

                  {servicesLoading ? (
                    <div className="flex items-center justify-center py-12">
                      <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
                      <span className="ml-2 text-slate-600">Loading services...</span>
                    </div>
                  ) : services.length > 0 ? (
                    <div className="space-y-4">
                      {services.map((service, index) => (
                        <div key={index} className="bg-slate-50 rounded-2xl p-6">
                          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                            <div>
                              <label className="block text-sm font-medium text-slate-700 mb-2">Service Name</label>
                              <input
                                type="text"
                                className="w-full p-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                                defaultValue={service.name}
                              />
                            </div>
                            <div>
                              <label className="block text-sm font-medium text-slate-700 mb-2">Duration (min)</label>
                              <input
                                type="number"
                                className="w-full p-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                                defaultValue={service.duration}
                              />
                            </div>
                            <div>
                              <label className="block text-sm font-medium text-slate-700 mb-2">Price (R)</label>
                              <input
                                type="number"
                                className="w-full p-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                                defaultValue={service.price}
                              />
                            </div>
                            <div className="flex items-end">
                              <button className="w-full p-3 text-red-600 hover:bg-red-50 rounded-lg transition-colors">
                                Remove
                              </button>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-center py-12">
                      <p className="text-slate-600 mb-4">
                        {isMockMode()
                          ? "No services available in mock mode"
                          : "No services found. Add your first service to get started."
                        }
                      </p>
                      <button className="bg-gradient-to-r from-blue-500 to-green-500 text-white px-4 py-2 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200">
                        + Add Your First Service
                      </button>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'availability' && (
                <div>
                  <h2 className="text-2xl font-bold text-slate-900 mb-6">Business Hours</h2>
                  <div className="space-y-4">
                    {weekDays.map((day) => (
                      <div key={day} className="bg-slate-50 rounded-2xl p-6">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center space-x-4">
                            <input
                              type="checkbox"
                              className="w-5 h-5 text-blue-600 rounded focus:ring-blue-500"
                              defaultChecked={day !== 'Sunday'}
                            />
                            <span className="font-medium text-slate-900 w-24">{day}</span>
                          </div>
                          <div className="flex items-center space-x-4">
                            <div className="flex items-center space-x-2">
                              <input
                                type="time"
                                className="p-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                                defaultValue="08:00"
                              />
                              <span className="text-slate-600">to</span>
                              <input
                                type="time"
                                className="p-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                                defaultValue="18:00"
                              />
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {activeTab === 'location' && (
                <div>
                  <div className="flex items-center gap-3 mb-6">
                    <h2 className="text-2xl font-bold text-slate-900">Business Location</h2>
                    {isMockMode() && (
                      <Badge variant="outline">Mock Mode</Badge>
                    )}
                  </div>

                  {businessLoading ? (
                    <div className="flex items-center justify-center py-12">
                      <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
                      <span className="ml-2 text-slate-600">Loading location information...</span>
                    </div>
                  ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div className="md:col-span-2">
                        <label className="block text-sm font-medium text-slate-700 mb-2">Street Address</label>
                        <input
                          type="text"
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={formData.streetAddress}
                          onChange={(e) => handleInputChange('streetAddress', e.target.value)}
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">City</label>
                        <input
                          type="text"
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={formData.city}
                          onChange={(e) => handleInputChange('city', e.target.value)}
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">Province</label>
                        <select
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={formData.province}
                          onChange={(e) => handleInputChange('province', e.target.value)}
                        >
                          <option value="">Select Province</option>
                          <option value="Gauteng">Gauteng</option>
                          <option value="Western Cape">Western Cape</option>
                          <option value="KwaZulu-Natal">KwaZulu-Natal</option>
                          <option value="Eastern Cape">Eastern Cape</option>
                          <option value="Free State">Free State</option>
                          <option value="Limpopo">Limpopo</option>
                          <option value="Mpumalanga">Mpumalanga</option>
                          <option value="North West">North West</option>
                          <option value="Northern Cape">Northern Cape</option>
                        </select>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">Postal Code</label>
                        <input
                          type="text"
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          value={formData.postalCode}
                          onChange={(e) => handleInputChange('postalCode', e.target.value)}
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-slate-700 mb-2">Google Maps Link</label>
                        <input
                          type="url"
                          className="w-full p-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                          placeholder="https://maps.google.com/..."
                          value={formData.googleMapsLink}
                          onChange={(e) => handleInputChange('googleMapsLink', e.target.value)}
                        />
                      </div>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'notifications' && (
                <div>
                  <h2 className="text-2xl font-bold text-slate-900 mb-6">Notification Preferences</h2>
                  <div className="space-y-6">
                    <div className="bg-slate-50 rounded-2xl p-6">
                      <h3 className="text-lg font-semibold text-slate-900 mb-4">Email Notifications</h3>
                      <div className="space-y-4">
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-medium text-slate-900">New Bookings</p>
                            <p className="text-sm text-slate-600">Get notified when clients book appointments</p>
                          </div>
                          <input type="checkbox" className="w-5 h-5 text-blue-600 rounded focus:ring-blue-500" defaultChecked />
                        </div>
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-medium text-slate-900">Booking Reminders</p>
                            <p className="text-sm text-slate-600">Daily summary of upcoming appointments</p>
                          </div>
                          <input type="checkbox" className="w-5 h-5 text-blue-600 rounded focus:ring-blue-500" defaultChecked />
                        </div>
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-medium text-slate-900">Cancellations</p>
                            <p className="text-sm text-slate-600">Get notified when bookings are cancelled</p>
                          </div>
                          <input type="checkbox" className="w-5 h-5 text-blue-600 rounded focus:ring-blue-500" defaultChecked />
                        </div>
                      </div>
                    </div>

                    <div className="bg-slate-50 rounded-2xl p-6">
                      <h3 className="text-lg font-semibold text-slate-900 mb-4">SMS Notifications</h3>
                      <div className="space-y-4">
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-medium text-slate-900">Urgent Notifications</p>
                            <p className="text-sm text-slate-600">Last-minute bookings and cancellations</p>
                          </div>
                          <input type="checkbox" className="w-5 h-5 text-blue-600 rounded focus:ring-blue-500" defaultChecked />
                        </div>
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-medium text-slate-900">Daily Reminders</p>
                            <p className="text-sm text-slate-600">Morning summary of today's appointments</p>
                          </div>
                          <input type="checkbox" className="w-5 h-5 text-blue-600 rounded focus:ring-blue-500" />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              <div className="mt-8 flex justify-end space-x-4">
                <button
                  className="px-6 py-3 text-slate-700 border border-slate-300 rounded-xl hover:bg-slate-50 transition-colors"
                  onClick={() => window.location.reload()}
                  disabled={isSaving}
                >
                  Cancel
                </button>
                <button
                  className="px-6 py-3 bg-gradient-to-r from-blue-500 to-green-500 text-white rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                  onClick={handleSaveChanges}
                  disabled={isSaving || businessLoading}
                >
                  {isSaving && <Loader2 className="h-4 w-4 animate-spin" />}
                  {isSaving ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Settings;
