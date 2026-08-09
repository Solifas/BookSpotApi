
import React from 'react';
import { Calendar, Clock, User, MapPin } from 'lucide-react';

interface BookingCardProps {
  clientName: string;
  service: string;
  date: string;
  time: string;
  location: string;
  status: 'upcoming' | 'completed' | 'cancelled';
}

const BookingCard = ({ clientName, service, date, time, location, status }: BookingCardProps) => {
  const statusColors = {
    upcoming: 'bg-blue-100 text-blue-800',
    completed: 'bg-green-100 text-green-800',
    cancelled: 'bg-red-100 text-red-800'
  };

  return (
    <div className="bg-white rounded-2xl p-6 shadow-lg border border-slate-100 hover:shadow-xl transition-all duration-300">
      <div className="flex justify-between items-start mb-4">
        <div>
          <h3 className="text-lg font-semibold text-slate-800">{service}</h3>
          <div className="flex items-center space-x-2 text-slate-600 mt-1">
            <User className="h-4 w-4" />
            <span>{clientName}</span>
          </div>
        </div>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColors[status]}`}>
          {status.charAt(0).toUpperCase() + status.slice(1)}
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-sm text-slate-600">
        <div className="flex items-center space-x-2">
          <Calendar className="h-4 w-4 text-blue-500" />
          <span>{date}</span>
        </div>
        <div className="flex items-center space-x-2">
          <Clock className="h-4 w-4 text-green-500" />
          <span>{time}</span>
        </div>
        <div className="flex items-center space-x-2">
          <MapPin className="h-4 w-4 text-red-500" />
          <span className="truncate">{location}</span>
        </div>
      </div>
    </div>
  );
};

export default BookingCard;
