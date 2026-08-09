import { MapPin, Star } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import type { Service } from '../data/servicesData';
import { useAuth } from '../contexts/AuthContext';

interface ServiceCardProps {
  service: Service;
}

const truncateDescription = (description: string, maxLength: number = 100): string => {
  if (description.length <= maxLength) return description;
  return description.slice(0, maxLength).trim() + '…';
};

const ServiceCard = ({ service }: ServiceCardProps) => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const handleCardClick = () => {
    if (!user) {
      navigate('/login');
    } else {
      navigate(`/book/${service.businessId || service.id}`);
    }
  };

  return (
    <div
      onClick={handleCardClick}
      className="bg-white rounded-lg md:rounded-xl shadow-md hover:shadow-lg transition-all duration-300 overflow-hidden group hover:scale-102 cursor-pointer"
    >
      <div className="w-full h-20 md:h-32 bg-gradient-to-br from-blue-400 to-green-400 flex items-center justify-center">
        <span className="text-white font-medium text-xs md:text-sm">{service.name}</span>
      </div>
      <div className="p-2 md:p-4">
        <div className="flex items-start justify-between mb-1">
          <h3 className="font-medium text-slate-900 text-xs md:text-sm leading-tight flex-1 pr-1">
            {service.name}
          </h3>
          <span className="text-blue-600 font-bold text-xs md:text-sm">{service.price}</span>
        </div>

        <p className="text-slate-600 text-xs mb-2 line-clamp-2">
          {truncateDescription(service.description)}
        </p>

        <div className="flex items-center justify-between mb-1">
          <div className="flex items-center space-x-1">
            <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
            <span className="text-xs font-medium text-slate-700">{service.rating}</span>
          </div>
          <span className="text-xs text-green-600 bg-green-100 px-1 py-0.5 rounded">
            Available
          </span>
        </div>

        <div className="flex items-center text-slate-500 text-xs">
          <MapPin className="h-3 w-3 mr-1" />
          <span className="truncate">{service.location}</span>
        </div>
      </div>
    </div>
  );
};

export default ServiceCard;
