
import React, { useState } from 'react';
import { Calendar, Clock, User, Phone, Mail, X } from 'lucide-react';
import { Service } from '../data/servicesData';
import { Calendar as CalendarComponent } from '@/components/ui/calendar';
import { Button } from '@/components/ui/button';
import { format } from 'date-fns';
import { createBooking } from '../services/bookingService';
import { toast } from '@/components/ui/use-toast';

interface BookingModalProps {
  service: Service | null;
  isVisible: boolean;
  onClose: () => void;
  onBookingConfirm: (bookingDetails: BookingDetails) => void;
}

interface BookingDetails {
  serviceId: string;
  date: Date;
  timeSlot: string;
  clientName: string;
  clientPhone: string;
  clientEmail: string;
}

const BookingModal = ({ service, isVisible, onClose, onBookingConfirm }: BookingModalProps) => {
  const [selectedDate, setSelectedDate] = useState<Date | undefined>(undefined);
  const [selectedTimeSlot, setSelectedTimeSlot] = useState<string>('');
  const [clientName, setClientName] = useState('');
  const [clientPhone, setClientPhone] = useState('');
  const [clientEmail, setClientEmail] = useState('');
  const [currentStep, setCurrentStep] = useState(1);
  const [loading, setLoading] = useState(false);

  // Mock available time slots - in real app this would come from the service provider's calendar
  const availableTimeSlots = [
    '09:00', '10:00', '11:00', '12:00', '14:00', '15:00', '16:00', '17:00'
  ];

  if (!isVisible || !service) return null;

  const handleDateSelect = (date: Date | undefined) => {
    setSelectedDate(date);
    setSelectedTimeSlot('');
  };

  const handleTimeSlotSelect = (timeSlot: string) => {
    setSelectedTimeSlot(timeSlot);
    setCurrentStep(2);
  };

  const handleBookingSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDate || !selectedTimeSlot || !clientName || !clientPhone || !clientEmail) {
      return;
    }

    const bookingDetails: BookingDetails = {
      serviceId: service.id,
      date: selectedDate,
      timeSlot: selectedTimeSlot,
      clientName,
      clientPhone,
      clientEmail
    };

    onBookingConfirm(bookingDetails);

    // Reset form
    setSelectedDate(undefined);
    setSelectedTimeSlot('');
    setClientName('');
    setClientPhone('');
    setClientEmail('');
    setCurrentStep(1);
  };

  const goBackToStep1 = () => {
    setCurrentStep(1);
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-hidden">
        <div className="p-6 border-b border-slate-200 flex justify-between items-center">
          <h2 className="text-2xl font-bold text-slate-900">Book Appointment</h2>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 text-2xl font-bold"
          >
            <X className="h-6 w-6" />
          </button>
        </div>

        <div className="p-6">
          {/* Service Info */}
          <div className="bg-slate-50 rounded-xl p-4 mb-6">
            <h3 className="text-lg font-semibold text-slate-900 mb-2">{service.name}</h3>
            <div className="flex items-center space-x-4 text-sm text-slate-600">
              <span className="font-medium text-xl text-slate-900">{service.price}</span>
              <span>{service.location}</span>
            </div>
            {service.type && (
              <div className="mt-2 text-sm text-slate-600">
                <span className="font-medium">Provider:</span> {service.type}
              </div>
            )}
          </div>

          {currentStep === 1 && (
            <div>
              <h4 className="text-lg font-semibold text-slate-900 mb-4">Select Date & Time</h4>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Calendar */}
                <div>
                  <h5 className="font-medium text-slate-700 mb-3">Choose a date</h5>
                  <CalendarComponent
                    mode="single"
                    selected={selectedDate}
                    onSelect={handleDateSelect}
                    disabled={(date) => date < new Date()}
                    className="rounded-md border"
                  />
                </div>

                {/* Time Slots */}
                <div>
                  <h5 className="font-medium text-slate-700 mb-3">Available times</h5>
                  {selectedDate ? (
                    <div className="grid grid-cols-2 gap-2">
                      {availableTimeSlots.map((timeSlot) => (
                        <button
                          key={timeSlot}
                          onClick={() => handleTimeSlotSelect(timeSlot)}
                          className="p-3 border border-slate-300 rounded-lg hover:bg-blue-50 hover:border-blue-300 transition-colors text-sm font-medium"
                        >
                          {timeSlot}
                        </button>
                      ))}
                    </div>
                  ) : (
                    <p className="text-slate-500 text-sm">Please select a date first</p>
                  )}
                </div>
              </div>
            </div>
          )}

          {currentStep === 2 && (
            <div>
              <div className="flex items-center justify-between mb-4">
                <h4 className="text-lg font-semibold text-slate-900">Your Details</h4>
                <button
                  onClick={goBackToStep1}
                  className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                >
                  ← Back to date selection
                </button>
              </div>

              {/* Selected Date & Time */}
              <div className="bg-blue-50 rounded-lg p-4 mb-6">
                <div className="flex items-center space-x-4 text-sm">
                  <div className="flex items-center space-x-2">
                    <Calendar className="h-4 w-4 text-blue-600" />
                    <span className="font-medium">
                      {selectedDate && format(selectedDate, 'EEEE, MMMM d, yyyy')}
                    </span>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Clock className="h-4 w-4 text-blue-600" />
                    <span className="font-medium">{selectedTimeSlot}</span>
                  </div>
                </div>
              </div>

              {/* Client Details Form */}
              <form onSubmit={handleBookingSubmit} className="space-y-4">
                <div>
                  <label htmlFor="clientName" className="block text-sm font-medium text-slate-700 mb-1">
                    Full Name *
                  </label>
                  <div className="relative">
                    <User className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
                    <input
                      type="text"
                      id="clientName"
                      value={clientName}
                      onChange={(e) => setClientName(e.target.value)}
                      className="w-full pl-10 pr-4 py-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      placeholder="Enter your full name"
                      required
                    />
                  </div>
                </div>

                <div>
                  <label htmlFor="clientPhone" className="block text-sm font-medium text-slate-700 mb-1">
                    Phone Number *
                  </label>
                  <div className="relative">
                    <Phone className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
                    <input
                      type="tel"
                      id="clientPhone"
                      value={clientPhone}
                      onChange={(e) => setClientPhone(e.target.value)}
                      className="w-full pl-10 pr-4 py-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      placeholder="+27 12 345 6789"
                      required
                    />
                  </div>
                </div>

                <div>
                  <label htmlFor="clientEmail" className="block text-sm font-medium text-slate-700 mb-1">
                    Email Address *
                  </label>
                  <div className="relative">
                    <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
                    <input
                      type="email"
                      id="clientEmail"
                      value={clientEmail}
                      onChange={(e) => setClientEmail(e.target.value)}
                      className="w-full pl-10 pr-4 py-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      placeholder="your.email@example.com"
                      required
                    />
                  </div>
                </div>

                <div className="flex space-x-4 pt-4">
                  <Button
                    type="button"
                    onClick={onClose}
                    variant="outline"
                    className="flex-1"
                  >
                    Cancel
                  </Button>
                  <Button
                    type="submit"
                    className="flex-1 bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600"
                  >
                    Confirm Booking
                  </Button>
                </div>
              </form>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default BookingModal;
