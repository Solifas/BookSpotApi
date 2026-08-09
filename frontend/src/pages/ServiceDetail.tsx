import { useParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { ArrowLeft, MapPin, Star, Phone, Mail, Clock, Tag } from 'lucide-react';
import Navigation from '../components/Navigation';
import { Button } from '../components/ui/button';
import type { Service } from '../data/servicesData';
import { SearchService } from '../services/searchService';

const ServiceDetail = () => {
  const { serviceId } = useParams<{ serviceId: string }>();
  const navigate = useNavigate();
  const [service, setService] = useState<Service | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadService = async () => {
      if (!serviceId) return;
      
      try {
        const response = await SearchService.searchServices(
          SearchService.buildSearchParams(undefined, undefined, undefined, undefined, undefined, 1, 100)
        );
        
        const foundService = response.services.find(s => s.id === serviceId);
        setService(foundService || null);
      } catch (error) {
        console.error('Failed to load service:', error);
        setService(null);
      } finally {
        setIsLoading(false);
      }
    };

    loadService();
  }, [serviceId]);

  const handleBookNow = () => {
    if (service?.providerId) {
      navigate(`/book/${service.providerId}`);
    } else {
      navigate('/book');
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
        <Navigation />
        <div className="max-w-4xl mx-auto px-4 py-8">
          <div className="animate-pulse">
            <div className="h-64 bg-slate-200 rounded-xl mb-6"></div>
            <div className="h-8 bg-slate-200 rounded w-1/2 mb-4"></div>
            <div className="h-4 bg-slate-200 rounded w-full mb-2"></div>
            <div className="h-4 bg-slate-200 rounded w-3/4"></div>
          </div>
        </div>
      </div>
    );
  }

  if (!service) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
        <Navigation />
        <div className="max-w-4xl mx-auto px-4 py-16 text-center">
          <h1 className="text-2xl font-bold text-slate-900 mb-4">Service Not Found</h1>
          <p className="text-slate-600 mb-6">The service you're looking for doesn't exist.</p>
          <Button onClick={() => navigate('/')}>Back to Home</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
      <Navigation />
      
      <div className="max-w-4xl mx-auto px-4 py-6">
        {/* Back Button */}
        <button
          onClick={() => navigate(-1)}
          className="flex items-center text-slate-600 hover:text-slate-900 mb-4 transition-colors"
        >
          <ArrowLeft className="h-5 w-5 mr-2" />
          Back
        </button>

        {/* Service Header */}
        <div className="bg-white rounded-2xl shadow-lg overflow-hidden mb-6">
          <div className="w-full h-48 md:h-64 bg-gradient-to-br from-blue-400 to-green-400 flex items-center justify-center">
            <span className="text-white font-bold text-2xl md:text-3xl">{service.type}</span>
          </div>
          
          <div className="p-6">
            <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-4 mb-4">
              <div>
                <h1 className="text-2xl md:text-3xl font-bold text-slate-900 mb-2">
                  {service.name}
                </h1>
                {service.providerName && (
                  <p className="text-slate-600">by {service.providerName}</p>
                )}
              </div>
              <div className="text-right">
                <span className="text-2xl md:text-3xl font-bold text-blue-600">{service.price}</span>
              </div>
            </div>

            <div className="flex flex-wrap gap-4 mb-6">
              <div className="flex items-center text-slate-600">
                <Star className="h-5 w-5 fill-yellow-400 text-yellow-400 mr-1" />
                <span className="font-medium">{service.rating}</span>
                <span className="text-slate-400 ml-1">({service.reviewCount} reviews)</span>
              </div>
              <div className="flex items-center text-slate-600">
                <MapPin className="h-5 w-5 mr-1 text-slate-400" />
                <span>{service.location}</span>
              </div>
              <div className="flex items-center text-green-600">
                <Clock className="h-5 w-5 mr-1" />
                <span>{service.availability}</span>
              </div>
            </div>

            <Button
              onClick={handleBookNow}
              className="w-full md:w-auto bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600 text-white px-8 py-3 text-lg"
            >
              Book Now
            </Button>
          </div>
        </div>

        {/* Description */}
        <div className="bg-white rounded-2xl shadow-lg p-6 mb-6">
          <h2 className="text-xl font-bold text-slate-900 mb-4">About This Service</h2>
          <p className="text-slate-600 leading-relaxed">{service.description}</p>
          
          {service.tags && service.tags.length > 0 && (
            <div className="mt-4">
              <div className="flex items-center gap-2 flex-wrap">
                <Tag className="h-4 w-4 text-slate-400" />
                {service.tags.map((tag, index) => (
                  <span
                    key={index}
                    className="bg-blue-100 text-blue-700 px-3 py-1 rounded-full text-sm"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Contact Information */}
        <div className="bg-white rounded-2xl shadow-lg p-6">
          <h2 className="text-xl font-bold text-slate-900 mb-4">Contact Information</h2>
          <div className="space-y-3">
            {service.phone && (
              <a
                href={`tel:${service.phone}`}
                className="flex items-center text-slate-600 hover:text-blue-600 transition-colors"
              >
                <Phone className="h-5 w-5 mr-3 text-slate-400" />
                {service.phone}
              </a>
            )}
            {service.email && (
              <a
                href={`mailto:${service.email}`}
                className="flex items-center text-slate-600 hover:text-blue-600 transition-colors"
              >
                <Mail className="h-5 w-5 mr-3 text-slate-400" />
                {service.email}
              </a>
            )}
            <div className="flex items-center text-slate-600">
              <MapPin className="h-5 w-5 mr-3 text-slate-400" />
              {service.location}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ServiceDetail;
