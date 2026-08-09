
import React, { useState } from 'react';
import { Calendar } from '@/components/ui/calendar';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Clock, MapPin, User } from 'lucide-react';
import { format } from 'date-fns';
import { useAuth } from '@/contexts/AuthContext';
import { useProviderBookings } from '@/hooks/useBookings';
import { BookingStatus } from '@/types/api';

const CalendarView = () => {
  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const { user } = useAuth();

  // Get all bookings for the provider
  const { data: allBookings = [], isLoading } = useProviderBookings(
    user?.id, // Fallback to default provider for demo
    undefined, // No status filter - get all bookings
    undefined, // No start date filter
    undefined  // No end date filter
    , user?.type === 'client'
  );

  // Filter bookings for the selected date based on createdAt
  const selectedDateKey = format(selectedDate, 'yyyy-MM-dd');
  const dayBookings = allBookings.filter(booking => {
    const bookingCreatedDate = format(new Date(booking.createdAt), 'yyyy-MM-dd');
    return bookingCreatedDate === selectedDateKey;
  });

  const getStatusColor = (status: BookingStatus | string) => {
    switch (status) {
      case BookingStatus.CONFIRMED:
      case 'CONFIRMED':
        return 'bg-green-100 text-green-800';
      case BookingStatus.PENDING:
      case 'PENDING':
        return 'bg-yellow-100 text-yellow-800';
      case BookingStatus.COMPLETED:
      case 'COMPLETED':
        return 'bg-blue-100 text-blue-800';
      case BookingStatus.CANCELLED:
      case 'CANCELLED':
        return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
      {/* Calendar Section */}
      <Card className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100">
        <CardHeader className="p-4 sm:p-6">
          <CardTitle className="text-lg sm:text-xl font-bold text-slate-900">Calendar</CardTitle>
        </CardHeader>
        <CardContent className="p-3 sm:p-6">
          <div className="flex justify-center">
            <Calendar
              mode="single"
              selected={selectedDate}
              onSelect={(date) => date && setSelectedDate(date)}
              className="rounded-lg border border-slate-200 bg-white shadow-sm scale-90 sm:scale-100"
            />
          </div>
        </CardContent>
      </Card>

      {/* Selected Day's Bookings */}
      <Card className="bg-white rounded-xl sm:rounded-2xl shadow-lg border border-slate-100">
        <CardHeader className="p-4 sm:p-6">
          <CardTitle className="text-lg sm:text-xl font-bold text-slate-900">
            <span className="hidden sm:inline">Bookings for {format(selectedDate, 'MMMM d, yyyy')}</span>
            <span className="sm:hidden">Bookings for {format(selectedDate, 'MMM d')}</span>
          </CardTitle>
        </CardHeader>
        <CardContent className="p-4 sm:p-6">
          <div className="max-h-[600px] overflow-y-auto">
            {isLoading ? (
              <div className="text-center py-8 sm:py-12">
                <div className="w-12 h-12 sm:w-16 sm:h-16 bg-slate-100 rounded-full flex items-center justify-center mx-auto mb-3 sm:mb-4 animate-pulse">
                  <Clock className="h-6 w-6 sm:h-8 sm:w-8 text-slate-400" />
                </div>
                <p className="text-slate-600 text-sm sm:text-base">Loading bookings...</p>
              </div>
            ) : dayBookings.length === 0 ? (
              <div className="text-center py-8 sm:py-12">
                <div className="w-12 h-12 sm:w-16 sm:h-16 bg-slate-100 rounded-full flex items-center justify-center mx-auto mb-3 sm:mb-4">
                  <Clock className="h-6 w-6 sm:h-8 sm:w-8 text-slate-400" />
                </div>
                <h3 className="text-base sm:text-lg font-medium text-slate-900 mb-2">No bookings scheduled</h3>
                <p className="text-slate-600 text-sm sm:text-base">No appointments are scheduled for this date.</p>
              </div>
            ) : (
              <div className="space-y-3 sm:space-y-4">
                {dayBookings.map((booking) => (
                  <div key={booking.id} className="bg-gradient-to-r from-slate-50 to-blue-50 rounded-lg sm:rounded-xl p-3 sm:p-5 border border-slate-100 hover:shadow-md transition-all duration-200">
                    <div className="flex items-center justify-between mb-2 sm:mb-3">
                      <div className="flex items-center gap-2 sm:gap-3">
                        <div className="w-8 h-8 sm:w-10 sm:h-10 bg-blue-100 rounded-full flex items-center justify-center">
                          <User className="h-4 w-4 sm:h-5 sm:w-5 text-blue-600" />
                        </div>
                        <div>
                          <h4 className="font-semibold text-slate-900 text-sm sm:text-base">{booking.client.fullName}</h4>
                          <p className="text-xs sm:text-sm text-slate-600">{booking.service.name}</p>
                        </div>
                      </div>
                      <Badge className={`${getStatusColor(booking.status)} font-medium text-xs`}>
                        {booking.status}
                      </Badge>
                    </div>
                    <div className="flex items-center gap-2 text-xs sm:text-sm text-slate-600 ml-10 sm:ml-13">
                      <div className="flex items-center gap-1 sm:gap-2">
                        <Clock className="h-3 w-3 sm:h-4 sm:w-4" />
                        <span className="font-medium">{format(new Date(booking.startTime), 'h:mm a')}</span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default CalendarView;
