import React, { useMemo, useState } from 'react';
import { Calendar, Users, Clock, TrendingUp, Check, X, Settings, CalendarDays, Loader2 } from 'lucide-react';
import { format } from 'date-fns';
import Navigation from '../components/Navigation';
import BookingCard from '../components/BookingCard';
import ClientList from '../components/ClientList';
import BusinessInsights from '../components/BusinessInsights';
import CalendarView from '../components/CalendarView';
import ServiceManagement from '../components/ServiceManagement';
import NewBookingModal from '../components/NewBookingModal';
import { toast } from '@/components/ui/use-toast';
import { useAuth } from '../contexts/AuthContext';
import { useProviderBookings, useUpdateBookingStatus, useClientBookings } from '../hooks/useBookings';
import { useFormattedDashboardStats } from '../hooks/useDashboard';
import { BookingStatus } from '../types/api';

const Dashboard = () => {
  const [activeTab, setActiveTab] = useState('overview');
  const { user } = useAuth();
  const providerId = user?.id;

  // Replace mock data with real API calls
  const { data: pendingBookings = [], isLoading: loadingPending, error: errorPending } = useProviderBookings(
    providerId,
    BookingStatus.PENDING,
    undefined,
    undefined,
    user?.type === 'client'
  );

  const { data: confirmedBookings = [], isLoading: loadingConfirmed, error: errorConfirmed } = useProviderBookings(
    providerId,
    BookingStatus.CONFIRMED,
    undefined,
    undefined,
    user?.type === 'client'
  );

  // Fetch all bookings when Requests tab is active
  const { data: allBookings = [], isLoading: loadingAllBookings, error: errorAllBookings, refetch: refetchAllBookings } = useProviderBookings(
    providerId,
    undefined, // No status filter - gets all bookings
    undefined,
    undefined,
    user?.type === 'client'
  );

  const showDeclineButton = useMemo(() => {
    // For clients, show decline button only for pending bookings
    if (user?.type === 'client') {
      return true;
    }
  }, [user]);

  // For clients, get their own bookings
  const { data: clientBookings = [], isLoading: loadingClientBookings, error: errorClientBookings } = useClientBookings(
    user?.type === 'client' ? (user?.id || '') : ''
  );

  const { formattedStats, isLoading: loadingStats, error: errorStats } = useFormattedDashboardStats(providerId, user?.type === 'client');

  const updateBookingStatus = useUpdateBookingStatus();

  const handleBookingAction = async (bookingId: string, action: 'accept' | 'decline') => {
    try {
      const status = action === 'accept' ? BookingStatus.CONFIRMED : BookingStatus.CANCELLED;
      const result = await updateBookingStatus.mutateAsync({ bookingId, status });

      // Check if booking was found and updated
      if (result === null) {
        toast({
          title: "Booking Not Found",
          description: "The booking could not be found. It may have been already updated or cancelled.",
          variant: "destructive",
        });
        return;
      }

      const booking = allBookings.find(b => b.id === bookingId) || pendingBookings.find(b => b.id === bookingId);
      if (booking) {
        toast({
          title: action === 'accept' ? "Booking Accepted" : "Booking Declined",
          description: `${booking.client.fullName}'s booking for ${booking.service.name} has been ${action === 'accept' ? 'confirmed' : 'declined'}.`,
          variant: action === 'accept' ? "default" : "destructive",
        });
      }
    } catch (error) {
      toast({
        title: "Error",
        description: `Failed to ${action} booking. Please try again.`,
        variant: "destructive",
      });
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      <Navigation />

      <div className="max-w-7xl mx-auto px-3 sm:px-4 lg:px-8 py-4 sm:py-8">
        {/* Header */}
        <div className="mb-6 sm:mb-8">
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 mb-2">
            Welcome back, {user?.name || 'Provider'}! 👋
          </h1>
          <p className="text-sm sm:text-base text-slate-600">Here's what's happening with your business today.</p>
        </div>

        {/* Tabs - Mobile Responsive */}
        <div className="mb-6 sm:mb-8">
          <div className="bg-slate-100 p-1 rounded-xl overflow-x-auto">
            <div className="flex space-x-1 min-w-max">
              {[
                { id: 'overview', label: 'Overview' },
                { id: 'bookings', label: 'Requests' },
                ...(user?.type === 'provider' ? [
                  { id: 'calendar', label: 'Calendar', icon: CalendarDays },
                  { id: 'services', label: 'Services', icon: Settings },
                  { id: 'clients', label: 'Clients' },
                  { id: 'insights', label: 'Insights' }
                ] : [])
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => {
                    setActiveTab(tab.id);
                    // Fetch all bookings when Requests tab is clicked
                    if (tab.id === 'bookings' && user?.type === 'provider') {
                      refetchAllBookings();
                    }
                  }}
                  className={`flex items-center gap-1 sm:gap-2 px-3 sm:px-6 py-2 rounded-lg font-medium transition-all duration-200 whitespace-nowrap text-sm sm:text-base ${activeTab === tab.id
                    ? 'bg-white text-blue-600 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                    }`}
                >
                  {tab.icon && <tab.icon className="h-3 w-3 sm:h-4 sm:w-4" />}
                  <span className="hidden xs:inline sm:inline">{tab.label}</span>
                  <span className="xs:hidden sm:hidden">{tab.label.slice(0, 4)}</span>
                  {tab.id === 'bookings' && allBookings.length > 0 && (
                    <span className="ml-1 sm:ml-2 bg-red-500 text-white text-xs px-1.5 sm:px-2 py-0.5 sm:py-1 rounded-full">
                      {allBookings.length}
                    </span>
                  )}
                </button>
              ))}
            </div>
          </div>
        </div>

        {activeTab === 'overview' && (
          <>
            {/* Stats Grid - Mobile Responsive */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-6 mb-6 sm:mb-8">
              {loadingStats ? (
                // Loading skeleton
                Array.from({ length: 4 }).map((_, index) => (
                  <div key={index} className="bg-white rounded-xl sm:rounded-2xl p-3 sm:p-6 shadow-lg border border-slate-100">
                    <div className="animate-pulse">
                      <div className="h-4 bg-slate-200 rounded mb-2"></div>
                      <div className="h-8 bg-slate-200 rounded"></div>
                    </div>
                  </div>
                ))
              ) : errorStats ? (
                // Error state
                <div className="col-span-full bg-red-50 border border-red-200 rounded-xl p-4">
                  <p className="text-red-700 text-sm">Failed to load dashboard statistics</p>
                </div>
              ) : formattedStats.length === 0 ? (
                // No data state
                <div className="col-span-full bg-slate-50 border border-slate-200 rounded-xl p-8 text-center">
                  <TrendingUp className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                  <p className="text-slate-600">No statistics available yet</p>
                  <p className="text-slate-500 text-sm mt-1">Start accepting bookings to see your stats</p>
                </div>
              ) : (
                // Actual stats
                formattedStats.map((stat, index) => (
                  <div key={index} className="bg-white rounded-xl sm:rounded-2xl p-3 sm:p-6 shadow-lg border border-slate-100 hover:shadow-xl transition-all duration-300">
                    <div className="flex flex-col sm:flex-row items-center sm:justify-between">
                      <div className="text-center sm:text-left mb-2 sm:mb-0">
                        <p className="text-slate-600 text-xs sm:text-sm font-medium">{stat.title}</p>
                        <p className="text-lg sm:text-2xl font-bold text-slate-900 mt-1">{stat.value}</p>
                      </div>
                      <div className={`w-8 h-8 sm:w-12 sm:h-12 ${stat.color} rounded-lg sm:rounded-xl flex items-center justify-center`}>
                        <Calendar className="h-4 w-4 sm:h-6 sm:w-6 text-white" />
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Main Content - Mobile Responsive */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 sm:gap-8">
              {/* Pending Booking Requests */}
              <div>
                <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
                  <div className="flex items-center justify-between mb-4 sm:mb-6">
                    <h2 className="text-lg sm:text-xl font-bold text-slate-900">Pending Requests</h2>
                    {pendingBookings.length > 0 && (
                      <span className="bg-red-500 text-white text-xs px-2 py-1 rounded-full">
                        {pendingBookings.length}
                      </span>
                    )}
                  </div>

                  {loadingPending ? (
                    <div className="space-y-4">
                      {Array.from({ length: 2 }).map((_, i) => (
                        <div key={i} className="animate-pulse bg-slate-100 rounded-lg h-24"></div>
                      ))}
                    </div>
                  ) : errorPending ? (
                    <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                      <p className="text-red-700 text-sm">Failed to load pending requests</p>
                    </div>
                  ) : pendingBookings.length === 0 ? (
                    <div className="text-center py-8">
                      <Clock className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                      <p className="text-slate-600">No pending requests</p>
                      <p className="text-slate-500 text-sm mt-1">New booking requests will appear here</p>
                    </div>
                  ) : (
                    <div className="space-y-3 sm:space-y-4 max-h-96 overflow-y-auto">
                      {pendingBookings.map((booking) => {
                        const isOwnBooking = booking.client.id === user?.id;
                        return (
                          <div key={booking.id} className="bg-slate-50 rounded-lg p-4">
                            <div className="flex items-start justify-between mb-2">
                              <div>
                                <h3 className="font-semibold text-slate-900">{booking.providerName}</h3>
                                <p className="text-sm text-slate-600">{booking.service.name}</p>
                              </div>
                              <span className="bg-yellow-100 text-yellow-700 text-xs px-2 py-1 rounded-full">
                                Pending
                              </span>
                            </div>
                            <p className="text-sm text-slate-500">
                              {new Date(booking.startTime).toLocaleDateString()} at {new Date(booking.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                            </p>
                            {!isOwnBooking ? (
                              <div className="flex gap-2 mt-3">
                                {new Date(booking.startTime) < new Date(new Date().getTime() + 4 * 60 * 60 * 1000) &&
                                  <button
                                    onClick={() => handleBookingAction(booking.id, 'decline')}
                                    className="flex-1 px-3 py-1.5 bg-red-100 text-red-700 rounded text-sm hover:bg-red-200 transition-colors"
                                  >
                                    Decline
                                  </button>
                                }

                                {user?.type !== 'client' &&
                                  <button
                                    onClick={() => handleBookingAction(booking.id, 'accept')}
                                    className="flex-1 px-3 py-1.5 bg-green-100 text-green-700 rounded text-sm hover:bg-green-200 transition-colors"
                                  >
                                    Accept
                                  </button>
                                }
                              </div>
                            ) : (
                              <p className="text-xs text-slate-500 mt-3 italic">You cannot accept your own booking request</p>
                            )}
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>

              {/* Confirmed Bookings */}
              <div>
                <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
                  <div className="flex items-center justify-between mb-4 sm:mb-6">
                    <h2 className="text-lg sm:text-xl font-bold text-slate-900">Confirmed Bookings</h2>
                    <button
                      onClick={() => setActiveTab('calendar')}
                      className="text-blue-600 font-medium hover:text-blue-700 transition-colors text-sm sm:text-base"
                    >
                      View All
                    </button>
                  </div>

                  {loadingConfirmed ? (
                    <div className="space-y-4">
                      {Array.from({ length: 3 }).map((_, i) => (
                        <div key={i} className="animate-pulse bg-slate-100 rounded-lg h-20"></div>
                      ))}
                    </div>
                  ) : errorConfirmed ? (
                    <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                      <p className="text-red-700 text-sm">Failed to load confirmed bookings</p>
                    </div>
                  ) : confirmedBookings.length === 0 ? (
                    <div className="text-center py-8">
                      <Calendar className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                      <p className="text-slate-600">No confirmed bookings</p>
                      <p className="text-slate-500 text-sm mt-1">Confirmed bookings will appear here</p>
                    </div>
                  ) : (
                    <div className="space-y-3 sm:space-y-4 max-h-96 overflow-y-auto">
                      {confirmedBookings.slice(0, 5).map((booking) => (
                        <BookingCard
                          key={booking.id}
                          clientName={user?.type === 'client' ? booking.business.businessName : booking.client.fullName}
                          service={booking.service.name}
                          date={new Date(booking.startTime).toLocaleDateString()}
                          time={new Date(booking.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          location={booking.business.city}
                          status="upcoming"
                        />
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Quick Actions for Providers */}
            {user?.type === 'provider' && (
              <div className="mt-6 sm:mt-8">
                <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
                  <h3 className="text-base sm:text-lg font-bold text-slate-900 mb-3 sm:mb-4">Quick Actions</h3>
                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 sm:gap-3">
                    <NewBookingModal />
                    <button
                      onClick={() => setActiveTab('calendar')}
                      className="bg-slate-100 text-slate-700 p-3 sm:p-4 rounded-lg sm:rounded-xl font-medium hover:bg-slate-200 transition-all duration-200 text-sm sm:text-base"
                    >
                      View Calendar
                    </button>
                    <button
                      onClick={() => setActiveTab('services')}
                      className="bg-slate-100 text-slate-700 p-3 sm:p-4 rounded-lg sm:rounded-xl font-medium hover:bg-slate-200 transition-all duration-200 text-sm sm:text-base"
                    >
                      Manage Services
                    </button>
                  </div>
                </div>
              </div>
            )}
          </>
        )}

        {activeTab === 'bookings' && (
          <div className="max-w-6xl">
            <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
              <h2 className="text-lg sm:text-xl font-bold text-slate-900 mb-4 sm:mb-6">
                {user?.type === 'client' ? 'My Bookings' : 'All Booking Requests'}
              </h2>

              {user?.type === 'client' ? (
                // Client view - show their own bookings
                <>
                  {loadingClientBookings ? (
                    <div className="space-y-4">
                      {Array.from({ length: 3 }).map((_, i) => (
                        <div key={i} className="animate-pulse bg-slate-100 rounded-lg h-32"></div>
                      ))}
                    </div>
                  ) : errorClientBookings ? (
                    <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                      <p className="text-red-700 text-sm">Failed to load your bookings</p>
                    </div>
                  ) : clientBookings.length === 0 ? (
                    <div className="text-center py-8 sm:py-12">
                      <Calendar className="h-8 w-8 sm:h-12 sm:w-12 text-slate-400 mx-auto mb-3 sm:mb-4" />
                      <p className="text-slate-600 text-sm sm:text-base">No bookings yet</p>
                      <p className="text-slate-500 text-xs sm:text-sm mt-1">Your bookings will appear here</p>
                    </div>
                  ) : (
                    <div className="space-y-4 sm:space-y-6">
                      {clientBookings.map((booking) => (
                        <div key={booking.id} className="bg-slate-50 rounded-lg sm:rounded-xl p-4 sm:p-6">
                          <div className="flex flex-col gap-4">
                            <div className="flex-1">
                              <div className="flex items-center gap-3 sm:gap-4 mb-3">
                                <div className="w-10 h-10 sm:w-12 sm:h-12 bg-blue-100 rounded-full flex items-center justify-center">
                                  <Users className="h-5 w-5 sm:h-6 sm:w-6 text-blue-600" />
                                </div>
                                <div>
                                  <h3 className="font-semibold text-slate-900 text-sm sm:text-base">{booking.business.businessName}</h3>
                                  <p className="text-slate-600 text-xs sm:text-sm">{booking.business.city}</p>
                                </div>
                                <div className="ml-auto">
                                  <span className={`px-2 py-1 rounded-full text-xs font-medium ${booking.status === 'pending' ? 'bg-yellow-100 text-yellow-700' :
                                    booking.status === 'confirmed' ? 'bg-green-100 text-green-700' :
                                      booking.status === 'cancelled' ? 'bg-red-100 text-red-700' :
                                        'bg-blue-100 text-blue-700'
                                    }`}>
                                    {booking.status.charAt(0).toUpperCase() + booking.status.slice(1)}
                                  </span>
                                </div>
                              </div>

                              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 sm:gap-4 text-xs sm:text-sm">
                                <div>
                                  <p className="text-slate-500 font-medium">Service</p>
                                  <p className="text-slate-900">{booking.service.name}</p>
                                </div>
                                <div>
                                  <p className="text-slate-500 font-medium">Date & Time</p>
                                  <p className="text-slate-900">
                                    {new Date(booking.startTime).toLocaleDateString()} at {new Date(booking.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-slate-500 font-medium">Duration</p>
                                  <p className="text-slate-900">{booking.service.durationMinutes} minutes</p>
                                </div>
                                <div>
                                  <p className="text-slate-500 font-medium">Price</p>
                                  <p className="text-slate-900 font-semibold">R{booking.service.price}</p>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </>
              ) : (
                // Provider view - show all booking requests sorted by date
                <>
                  {loadingAllBookings ? (
                    <div className="space-y-4">
                      {Array.from({ length: 3 }).map((_, i) => (
                        <div key={i} className="animate-pulse bg-slate-100 rounded-lg h-32"></div>
                      ))}
                    </div>
                  ) : errorAllBookings ? (
                    <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                      <p className="text-red-700 text-sm">Failed to load booking requests</p>
                    </div>
                  ) : allBookings.length === 0 ? (
                    <div className="text-center py-8 sm:py-12">
                      <Calendar className="h-8 w-8 sm:h-12 sm:w-12 text-slate-400 mx-auto mb-3 sm:mb-4" />
                      <p className="text-slate-600 text-sm sm:text-base">No booking requests</p>
                      <p className="text-slate-500 text-xs sm:text-sm mt-1">Booking requests will appear here</p>
                    </div>
                  ) : (
                    <div className="space-y-4 sm:space-y-6">
                      {[...allBookings].sort((a, b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime()).map((booking) => (
                        <div key={booking.id} className="bg-slate-50 rounded-lg sm:rounded-xl p-4 sm:p-6">
                          <div className="flex flex-col gap-4">
                            <div className="flex-1">
                              <div className="flex items-center gap-3 sm:gap-4 mb-3">
                                <div className="w-10 h-10 sm:w-12 sm:h-12 bg-blue-100 rounded-full flex items-center justify-center">
                                  <Users className="h-5 w-5 sm:h-6 sm:w-6 text-blue-600" />
                                </div>
                                <div>
                                  <h3 className="font-semibold text-slate-900 text-sm sm:text-base">{booking.client.fullName}</h3>
                                  <p className="text-slate-600 text-xs sm:text-sm">{booking.client.email}</p>
                                </div>
                              </div>

                              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4 text-xs sm:text-sm">
                                <div>
                                  <p className="text-slate-500 font-medium">Service</p>
                                  <p className="text-slate-900">{booking.service.name}</p>
                                </div>
                                <div>
                                  <p className="text-slate-500 font-medium">Date & Time</p>
                                  <p className="text-slate-900">
                                    {format(new Date(booking.startTime), 'dd-MMMM-yyyy')} at {new Date(booking.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-slate-500 font-medium">Phone</p>
                                  <p className="text-slate-900">{booking.client.contactNumber || 'Not provided'}</p>
                                </div>
                                <div>
                                  <p className="text-slate-500 font-medium">Price</p>
                                  <p className="text-slate-900 font-semibold">R{booking.service.price}</p>
                                </div>
                                <div>
                                  <p className="text-slate-500 font-medium">Status</p>
                                  <span className={`inline-block px-2 py-1 rounded-full text-xs font-medium ${booking.status === 'pending' ? 'bg-yellow-100 text-yellow-700' :
                                    booking.status === 'confirmed' ? 'bg-green-100 text-green-700' :
                                      booking.status === 'cancelled' ? 'bg-red-100 text-red-700' :
                                        'bg-blue-100 text-blue-700'
                                    }`}>
                                    {booking.status.charAt(0).toUpperCase() + booking.status.slice(1)}
                                  </span>
                                </div>
                              </div>
                            </div>

                            {booking.status === 'pending' && (
                              <div className="flex gap-2 sm:gap-3">
                                <button
                                  onClick={() => handleBookingAction(booking.id, 'decline')}
                                  disabled={updateBookingStatus.isPending}
                                  className="flex-1 sm:flex-none flex items-center justify-center gap-2 px-3 sm:px-4 py-2 bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors text-sm disabled:opacity-50"
                                >
                                  {updateBookingStatus.isPending ? (
                                    <Loader2 className="h-3 w-3 sm:h-4 sm:w-4 animate-spin" />
                                  ) : (
                                    <X className="h-3 w-3 sm:h-4 sm:w-4" />
                                  )}
                                  Decline
                                </button>
                                <button
                                  onClick={() => handleBookingAction(booking.id, 'accept')}
                                  disabled={updateBookingStatus.isPending}
                                  className="flex-1 sm:flex-none flex items-center justify-center gap-2 px-3 sm:px-4 py-2 bg-green-100 text-green-700 rounded-lg hover:bg-green-200 transition-colors text-sm disabled:opacity-50"
                                >
                                  {updateBookingStatus.isPending ? (
                                    <Loader2 className="h-3 w-3 sm:h-4 sm:w-4 animate-spin" />
                                  ) : (
                                    <Check className="h-3 w-3 sm:h-4 sm:w-4" />
                                  )}
                                  Accept
                                </button>
                              </div>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        )}

        {activeTab === 'calendar' && (
          <div className="max-w-6xl">
            <CalendarView />
          </div>
        )}

        {activeTab === 'services' && (
          <div className="max-w-6xl">
            <ServiceManagement />
          </div>
        )}

        {activeTab === 'clients' && (
          <div className="max-w-4xl">
            <ClientList />
          </div>
        )}

        {activeTab === 'insights' && (
          <div className="max-w-6xl">
            <BusinessInsights />
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
