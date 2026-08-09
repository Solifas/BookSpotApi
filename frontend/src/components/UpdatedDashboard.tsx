// Updated Dashboard component - Example of replacing mock data with TanStack Query
import React, { useState } from 'react';
import { Calendar, Users, Clock, TrendingUp, Check, X, Settings, CalendarDays } from 'lucide-react';
import Navigation from './Navigation';
import BookingCard from './BookingCard';
import { toast } from '@/components/ui/use-toast';
import { useAuth } from '../contexts/AuthContext';
import { useProviderBookings, useUpdateBookingStatus } from '../hooks/useBookings';
import { useFormattedDashboardStats } from '../hooks/useDashboard';
import { BookingStatus } from '../types/api';

const UpdatedDashboard = () => {
    const [activeTab, setActiveTab] = useState('overview');
    const { user } = useAuth();

    // Replace mock data with real API calls
    const { data: pendingBookings = [], isLoading: loadingPending } = useProviderBookings(
        user?.id || '',
        BookingStatus.PENDING,
        undefined,
        undefined,
        user?.type === 'client'
    );

    const { data: confirmedBookings = [], isLoading: loadingConfirmed } = useProviderBookings(
        user?.id || '',
        BookingStatus.CONFIRMED,
        undefined,
        undefined,
        user?.type === 'client'
    );

    const { formattedStats, isLoading: loadingStats } = useFormattedDashboardStats(user?.id || '');

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

            const booking = pendingBookings.find(b => b.id === bookingId);
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
                        Welcome back, {user?.name}! 👋
                    </h1>
                    <p className="text-sm sm:text-base text-slate-600">Here's what's happening with your business today.</p>
                </div>

                {/* Tabs */}
                <div className="mb-6 sm:mb-8">
                    <div className="bg-slate-100 p-1 rounded-xl overflow-x-auto">
                        <div className="flex space-x-1 min-w-max">
                            {[
                                { id: 'overview', label: 'Overview' },
                                { id: 'bookings', label: 'Requests' },
                                { id: 'calendar', label: 'Calendar', icon: CalendarDays },
                                { id: 'services', label: 'Services', icon: Settings },
                                { id: 'clients', label: 'Clients' },
                                { id: 'insights', label: 'Insights' }
                            ].map((tab) => (
                                <button
                                    key={tab.id}
                                    onClick={() => setActiveTab(tab.id)}
                                    className={`flex items-center gap-1 sm:gap-2 px-3 sm:px-6 py-2 rounded-lg font-medium transition-all duration-200 whitespace-nowrap text-sm sm:text-base ${activeTab === tab.id
                                        ? 'bg-white text-blue-600 shadow-sm'
                                        : 'text-slate-600 hover:text-slate-900'
                                        }`}
                                >
                                    {tab.icon && <tab.icon className="h-3 w-3 sm:h-4 sm:w-4" />}
                                    <span className="hidden xs:inline sm:inline">{tab.label}</span>
                                    <span className="xs:hidden sm:hidden">{tab.label.slice(0, 4)}</span>
                                    {tab.id === 'bookings' && pendingBookings.length > 0 && (
                                        <span className="ml-1 sm:ml-2 bg-red-500 text-white text-xs px-1.5 sm:px-2 py-0.5 sm:py-1 rounded-full">
                                            {pendingBookings.length}
                                        </span>
                                    )}
                                </button>
                            ))}
                        </div>
                    </div>
                </div>

                {activeTab === 'overview' && (
                    <>
                        {/* Stats Grid */}
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
                            ) : (
                                formattedStats.map((stat, index) => (
                                    <div key={index} className="bg-white rounded-xl sm:rounded-2xl p-3 sm:p-6 shadow-lg border border-slate-100 hover:shadow-xl transition-all duration-300">
                                        <div className="flex flex-col sm:flex-row items-center sm:justify-between">
                                            <div className="text-center sm:text-left mb-2 sm:mb-0">
                                                <p className="text-slate-600 text-xs sm:text-sm font-medium">{stat.title}</p>
                                                <p className="text-lg sm:text-2xl font-bold text-slate-900 mt-1">{stat.value}</p>
                                            </div>
                                            <div className={`w-8 h-8 sm:w-12 sm:h-12 ${stat.color} rounded-lg sm:rounded-xl flex items-center justify-center`}>
                                                {/* Icon would be rendered here based on stat.icon */}
                                                <Calendar className="h-4 w-4 sm:h-6 sm:w-6 text-white" />
                                            </div>
                                        </div>
                                    </div>
                                ))
                            )}
                        </div>

                        {/* Main Content */}
                        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8">
                            {/* Confirmed Bookings */}
                            <div className="lg:col-span-2">
                                <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
                                    <div className="flex items-center justify-between mb-4 sm:mb-6">
                                        <h2 className="text-lg sm:text-xl font-bold text-slate-900">Confirmed Bookings</h2>
                                        <button className="text-blue-600 font-medium hover:text-blue-700 transition-colors text-sm sm:text-base">
                                            View All
                                        </button>
                                    </div>

                                    {loadingConfirmed ? (
                                        <div className="space-y-4">
                                            {Array.from({ length: 3 }).map((_, i) => (
                                                <div key={i} className="animate-pulse bg-slate-100 rounded-lg h-20"></div>
                                            ))}
                                        </div>
                                    ) : confirmedBookings.length === 0 ? (
                                        <div className="text-center py-8">
                                            <Calendar className="h-12 w-12 text-slate-400 mx-auto mb-4" />
                                            <p className="text-slate-600">No confirmed bookings</p>
                                        </div>
                                    ) : (
                                        <div className="space-y-3 sm:space-y-4">
                                            {confirmedBookings.slice(0, 5).map((booking) => (
                                                <BookingCard
                                                    key={booking.id}
                                                    clientName={booking.client.fullName}
                                                    service={booking.service.name}
                                                    date={new Date(booking.startTime).toLocaleDateString()}
                                                    time={new Date(booking.startTime).toLocaleTimeString()}
                                                    location={booking.business.city}
                                                    status="upcoming"
                                                />
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </div>

                            {/* Quick Actions */}
                            <div className="space-y-4 sm:space-y-6">
                                <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
                                    <h3 className="text-base sm:text-lg font-bold text-slate-900 mb-3 sm:mb-4">Quick Actions</h3>
                                    <div className="space-y-2 sm:space-y-3">
                                        <button className="w-full bg-gradient-to-r from-blue-500 to-green-500 text-white p-3 sm:p-4 rounded-lg sm:rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl text-sm sm:text-base">
                                            + New Booking
                                        </button>
                                        <button
                                            onClick={() => setActiveTab('calendar')}
                                            className="w-full bg-slate-100 text-slate-700 p-3 sm:p-4 rounded-lg sm:rounded-xl font-medium hover:bg-slate-200 transition-all duration-200 text-sm sm:text-base"
                                        >
                                            View Calendar
                                        </button>
                                        <button
                                            onClick={() => setActiveTab('services')}
                                            className="w-full bg-slate-100 text-slate-700 p-3 sm:p-4 rounded-lg sm:rounded-xl font-medium hover:bg-slate-200 transition-all duration-200 text-sm sm:text-base"
                                        >
                                            Manage Services
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </>
                )}

                {activeTab === 'bookings' && (
                    <div className="max-w-6xl">
                        <div className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
                            <h2 className="text-lg sm:text-xl font-bold text-slate-900 mb-4 sm:mb-6">Booking Requests</h2>

                            {loadingPending ? (
                                <div className="space-y-4">
                                    {Array.from({ length: 3 }).map((_, i) => (
                                        <div key={i} className="animate-pulse bg-slate-100 rounded-lg h-32"></div>
                                    ))}
                                </div>
                            ) : pendingBookings.length === 0 ? (
                                <div className="text-center py-8 sm:py-12">
                                    <Calendar className="h-8 w-8 sm:h-12 sm:w-12 text-slate-400 mx-auto mb-3 sm:mb-4" />
                                    <p className="text-slate-600 text-sm sm:text-base">No pending booking requests</p>
                                </div>
                            ) : (
                                <div className="space-y-4 sm:space-y-6">
                                    {pendingBookings.map((booking) => (
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
                                                                {new Date(booking.startTime).toLocaleDateString()} at {new Date(booking.startTime).toLocaleTimeString()}
                                                            </p>
                                                        </div>
                                                        <div>
                                                            <p className="text-slate-500 font-medium">Phone</p>
                                                            <p className="text-slate-900">{booking.client.contactNumber}</p>
                                                        </div>
                                                        <div>
                                                            <p className="text-slate-500 font-medium">Price</p>
                                                            <p className="text-slate-900 font-semibold">R{booking.service.price}</p>
                                                        </div>
                                                    </div>
                                                </div>

                                                <div className="flex gap-2 sm:gap-3">
                                                    <button
                                                        onClick={() => handleBookingAction(booking.id, 'decline')}
                                                        disabled={updateBookingStatus.isPending}
                                                        className="flex-1 sm:flex-none flex items-center justify-center gap-2 px-3 sm:px-4 py-2 bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors text-sm disabled:opacity-50"
                                                    >
                                                        <X className="h-3 w-3 sm:h-4 sm:w-4" />
                                                        Decline
                                                    </button>
                                                    <button
                                                        onClick={() => handleBookingAction(booking.id, 'accept')}
                                                        disabled={updateBookingStatus.isPending}
                                                        className="flex-1 sm:flex-none flex items-center justify-center gap-2 px-3 sm:px-4 py-2 bg-green-100 text-green-700 rounded-lg hover:bg-green-200 transition-colors text-sm disabled:opacity-50"
                                                    >
                                                        <Check className="h-3 w-3 sm:h-4 sm:w-4" />
                                                        Accept
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* Other tabs would be implemented similarly */}
            </div>
        </div>
    );
};

export default UpdatedDashboard;