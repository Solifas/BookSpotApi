import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Star, MapPin, Clock, Phone } from 'lucide-react';
import type { Service } from '../data/servicesData';
import BookingModal from './BookingModal';
import { toast } from '@/components/ui/use-toast';
import { useAuth } from '../contexts/AuthContext';

interface SearchResultsProps {
  services: Service[];
  isVisible: boolean;
  onClose: () => void;
}

interface BookingDetails {
  serviceId: string;
  date: Date;
  timeSlot: string;
  clientName: string;
  clientPhone: string;
  clientEmail: string;
}

const SearchResults = ({ services, isVisible, onClose }: SearchResultsProps) => {
  const navigate = useNavigate();
  const { isLoggedIn } = useAuth();
  const [selectedService, setSelectedService] = useState<Service | null>(null);
  const [showBookingModal, setShowBookingModal] = useState(false);

  if (!isVisible) return null;

  const handleBookNow = (service: Service) => {
    if (!isLoggedIn) {
      navigate('/login');
      return;
    }
    setSelectedService(service);
    setShowBookingModal(true);
  };

  const handleBookingConfirm = (bookingDetails: BookingDetails) => {
    console.log('Booking confirmed:', bookingDetails);
    
    // Show success toast
    toast({
      title: "Booking Confirmed!",
      description: `Your appointment with ${selectedService?.name} has been booked for ${bookingDetails.date.toLocaleDateString()} at ${bookingDetails.timeSlot}.`,
    });

    // Close modals
    setShowBookingModal(false);
    setSelectedService(null);
    onClose();
  };

  const handleCloseBookingModal = () => {
    setShowBookingModal(false);
    setSelectedService(null);
  };

  return (
    <>
      <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-start justify-center pt-20">
        <div className="bg-white rounded-2xl shadow-2xl max-w-4xl w-full mx-4 max-h-[80vh] overflow-hidden">
          <div className="p-6 border-b border-slate-200 flex justify-between items-center">
            <h2 className="text-2xl font-bold text-slate-900">Search Results</h2>
            <button
              onClick={onClose}
              className="text-slate-400 hover:text-slate-600 text-2xl font-bold"
            >
              ×
            </button>
          </div>
          
          <div className="overflow-y-auto max-h-[60vh]">
            {services.length === 0 ? (
              <div className="p-8 text-center">
                <p className="text-slate-600 text-lg">No services found matching your criteria.</p>
                <p className="text-slate-400 mt-2">Try adjusting your search terms or location.</p>
              </div>
            ) : (
              <div className="p-6 space-y-4">
                {services.map((service) => (
                  <div key={service.id} className="bg-slate-50 rounded-xl p-6 hover:bg-slate-100 transition-colors">
                    <div className="flex justify-between items-start mb-4">
                      <div className="flex-1">
                        <h3 className="text-xl font-semibold text-slate-900 mb-2">{service.name}</h3>
                        <p className="text-slate-600 mb-3">{service.description}</p>
                        
                        <div className="flex items-center space-x-4 text-sm text-slate-500 mb-3">
                          <div className="flex items-center space-x-1">
                            <MapPin className="h-4 w-4" />
                            <span>{service.location}</span>
                          </div>
                          <div className="flex items-center space-x-1">
                            <Clock className="h-4 w-4" />
                            <span>{service.availability}</span>
                          </div>
                          <div className="flex items-center space-x-1">
                            <Phone className="h-4 w-4" />
                            <span>{service.phone}</span>
                          </div>
                        </div>
                        
                        <div className="flex items-center space-x-4">
                          <div className="flex items-center space-x-1">
                            <div className="flex items-center">
                              {[...Array(5)].map((_, i) => (
                                <Star
                                  key={i}
                                  className={`h-4 w-4 ${
                                    i < Math.floor(service.rating)
                                      ? 'fill-yellow-400 text-yellow-400'
                                      : 'text-slate-300'
                                  }`}
                                />
                              ))}
                            </div>
                            <span className="text-sm text-slate-600">
                              {service.rating} ({service.reviewCount} reviews)
                            </span>
                          </div>
                          <span className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm font-medium">
                            {service.type}
                          </span>
                        </div>
                      </div>
                      
                      <div className="text-right ml-6">
                        <div className="text-2xl font-bold text-slate-900 mb-2">{service.price}</div>
                        <button 
                          onClick={() => handleBookNow(service)}
                          className="bg-gradient-to-r from-blue-500 to-green-500 text-white px-6 py-2 rounded-xl font-semibold hover:from-blue-600 hover:to-green-600 transition-all duration-200"
                        >
                          Book Now
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Booking Modal */}
      <BookingModal
        service={selectedService}
        isVisible={showBookingModal}
        onClose={handleCloseBookingModal}
        onBookingConfirm={handleBookingConfirm}
      />
    </>
  );
};

export default SearchResults;

