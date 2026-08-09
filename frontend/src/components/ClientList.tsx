
import React, { useState, useEffect } from 'react';
import { User, Phone, Mail, Calendar, MoreVertical, MessageCircle, Edit, Trash2, UserCheck } from 'lucide-react';
import { toast } from '@/components/ui/use-toast';
import NewClientModal from './NewClientModal';
import { useClients } from '@/hooks/useClients';

interface Client {
  id: string;
  fullName: string;
  email: string;
  contactNumber?: string;
  totalBookings: number;
  lastVisit: string;
  avatar?: string;
}

const ClientList = () => {
  const { data: clients = [], isLoading, error } = useClients();
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);

  useEffect(() => {
    if (!error) return;

    toast({
      title: 'Failed to Load Clients',
      description: error instanceof Error ? error.message : 'Something went wrong while fetching clients.',
      variant: 'destructive',
    });
  }, [error]);

  const handleMessage = (client: Client) => {
    toast({
      title: "Message Client",
      description: `Opening message thread with ${client.fullName}`,
    });
    console.log('Messaging client:', client);
  };

  const handleBooking = (client: Client) => {
    toast({
      title: "Book Appointment",
      description: `Creating new booking for ${client.fullName}`,
    });
    console.log('Booking appointment for client:', client);
  };

  const handleEditClient = (client: Client) => {
    toast({
      title: "Edit Client",
      description: `Opening edit form for ${client.fullName}`,
    });
    console.log('Editing client:', client);
    setOpenDropdown(null);
  };

  const handleDeleteClient = (client: Client) => {
    toast({
      title: "Delete Client",
      description: `Are you sure you want to delete ${client.fullName}?`,
      variant: "destructive",
    });
    console.log('Deleting client:', client);
    setOpenDropdown(null);
  };

  const handleViewProfile = (client: Client) => {
    toast({
      title: "View Profile",
      description: `Opening profile for ${client.fullName}`,
    });
    console.log('Viewing client profile:', client);
    setOpenDropdown(null);
  };

  const toggleDropdown = (clientId: string) => {
    setOpenDropdown(openDropdown === clientId ? null : clientId);
  };

  const handleAddClient = (newClient: Client) => {
    // setClients(prev => [...prev, newClient]);
  };

  return (
    <div className="bg-white rounded-2xl shadow-lg border border-slate-100 p-4 sm:p-6">
      <div className="flex items-center justify-between mb-4 sm:mb-6">
        <h2 className="text-lg sm:text-xl font-bold text-slate-900">Client Management</h2>
        <button className="text-blue-600 font-medium hover:text-blue-700 transition-colors text-sm sm:text-base">
          View All Clients
        </button>
      </div>

      <div className="space-y-3 sm:space-y-4">
        {isLoading && (
          Array.from({ length: 4 }).map((_, index) => (
            <div
              key={`client-skeleton-${index}`}
              className="animate-pulse p-3 sm:p-4 rounded-xl border border-slate-200 bg-slate-50 h-32"
            />
          ))
        )}

        {!isLoading && error && (
          <div className="bg-red-50 border border-red-200 rounded-xl p-4 text-sm text-red-700">
            Unable to load clients right now.
          </div>
        )}

        {!isLoading && !error && clients.length === 0 && (
          <div className="bg-slate-50 border border-slate-200 rounded-xl p-6 text-center text-sm text-slate-600">
            No clients found yet.
          </div>
        )}

        {!isLoading && !error && clients.map((client) => (
          <div key={client.id} className="p-3 sm:p-4 rounded-xl border border-slate-200 hover:bg-slate-50 transition-colors">
            {/* Mobile Layout - Stack everything vertically */}
            <div className="block sm:hidden">
              <div className="flex items-center space-x-3 mb-3">
                <div className="w-10 h-10 bg-gradient-to-br from-blue-400 to-green-400 rounded-full flex items-center justify-center text-white font-semibold flex-shrink-0">
                  {client.fullName.split(' ').map(n => n[0]).join('')}
                </div>
                <div className="flex-1 min-w-0">
                  <h3 className="font-semibold text-slate-900 text-sm">{client.fullName}</h3>
                </div>
              </div>

              <div className="space-y-2 text-xs text-slate-600 mb-3">
                <div className="flex items-center space-x-1">
                  <Mail className="h-3 w-3 flex-shrink-0" />
                  <span className="truncate">{client.email}</span>
                </div>
                <div className="flex items-center space-x-1">
                  <Phone className="h-3 w-3 flex-shrink-0" />
                  <span>{client.contactNumber ?? 'N/A'}</span>
                </div>
              </div>

              <div className="flex items-center justify-between mb-3">
                <div className="text-xs text-slate-600">
                  <span className="font-medium">{client.totalBookings}</span> bookings
                </div>
                <div className="text-xs text-slate-600">
                  Last: <span className="font-medium">{new Date(client.lastVisit).toLocaleDateString('en-ZA')}</span>
                </div>
              </div>

              <div className="flex items-center justify-center space-x-4">
                <button
                  onClick={() => handleMessage(client)}
                  className="flex items-center space-x-1 px-3 py-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors text-xs"
                  title="Send Message"
                >
                  <MessageCircle className="h-3 w-3" />
                  <span>Message</span>
                </button>
                <button
                  onClick={() => handleBooking(client)}
                  className="flex items-center space-x-1 px-3 py-2 text-green-600 hover:bg-green-50 rounded-lg transition-colors text-xs"
                  title="Book Appointment"
                >
                  <Calendar className="h-3 w-3" />
                  <span>Book</span>
                </button>
                <div className="relative">
                  <button
                    onClick={() => toggleDropdown(client.id)}
                    className="flex items-center space-x-1 px-3 py-2 text-slate-600 hover:bg-slate-100 rounded-lg transition-colors text-xs"
                    title="More Options"
                  >
                    <MoreVertical className="h-3 w-3" />
                    <span>More</span>
                  </button>

                  {openDropdown === client.id && (
                    <div className="absolute right-0 mt-2 w-40 bg-white rounded-lg shadow-lg border border-slate-200 z-20">
                      <div className="py-1">
                        <button
                          onClick={() => handleViewProfile(client)}
                          className="flex items-center space-x-2 w-full px-3 py-2 text-xs text-slate-700 hover:bg-slate-50 transition-colors"
                        >
                          <UserCheck className="h-3 w-3" />
                          <span>View Profile</span>
                        </button>
                        <button
                          onClick={() => handleEditClient(client)}
                          className="flex items-center space-x-2 w-full px-3 py-2 text-xs text-slate-700 hover:bg-slate-50 transition-colors"
                        >
                          <Edit className="h-3 w-3" />
                          <span>Edit Client</span>
                        </button>
                        <button
                          onClick={() => handleDeleteClient(client)}
                          className="flex items-center space-x-2 w-full px-3 py-2 text-xs text-red-600 hover:bg-red-50 transition-colors"
                        >
                          <Trash2 className="h-3 w-3" />
                          <span>Delete Client</span>
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Desktop Layout - Original horizontal layout */}
            <div className="hidden sm:block">
              <div className="flex items-start justify-between">
                <div className="flex items-center space-x-4 flex-1">
                  <div className="w-12 h-12 bg-gradient-to-br from-blue-400 to-green-400 rounded-full flex items-center justify-center text-white font-semibold flex-shrink-0">
                    {client.fullName.split(' ').map(n => n[0]).join('')}
                  </div>
                  <div className="flex-1 min-w-0">
                    <h3 className="font-semibold text-slate-900">{client.fullName}</h3>
                    <div className="flex flex-col sm:flex-row sm:items-center sm:space-x-4 text-sm text-slate-600 mt-1 space-y-1 sm:space-y-0">
                      <div className="flex items-center space-x-1">
                        <Mail className="h-3 w-3 flex-shrink-0" />
                        <span className="truncate">{client.email}</span>
                      </div>
                      <div className="flex items-center space-x-1">
                        <Phone className="h-3 w-3 flex-shrink-0" />
                        <span>{client.contactNumber ?? 'N/A'}</span>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="flex items-center space-x-2 ml-4">
                  <button
                    onClick={() => handleMessage(client)}
                    className="p-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors flex-shrink-0"
                    title="Send Message"
                  >
                    <MessageCircle className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => handleBooking(client)}
                    className="p-2 text-green-600 hover:bg-green-50 rounded-lg transition-colors flex-shrink-0"
                    title="Book Appointment"
                  >
                    <Calendar className="h-4 w-4" />
                  </button>
                  <div className="relative">
                    <button
                      onClick={() => toggleDropdown(client.id)}
                      className="p-2 text-slate-600 hover:bg-slate-100 rounded-lg transition-colors flex-shrink-0"
                      title="More Options"
                    >
                      <MoreVertical className="h-4 w-4" />
                    </button>

                    {openDropdown === client.id && (
                      <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-slate-200 z-10">
                        <div className="py-1">
                          <button
                            onClick={() => handleViewProfile(client)}
                            className="flex items-center space-x-2 w-full px-4 py-2 text-sm text-slate-700 hover:bg-slate-50 transition-colors"
                          >
                            <UserCheck className="h-4 w-4" />
                            <span>View Profile</span>
                          </button>
                          <button
                            onClick={() => handleEditClient(client)}
                            className="flex items-center space-x-2 w-full px-4 py-2 text-sm text-slate-700 hover:bg-slate-50 transition-colors"
                          >
                            <Edit className="h-4 w-4" />
                            <span>Edit Client</span>
                          </button>
                          <button
                            onClick={() => handleDeleteClient(client)}
                            className="flex items-center space-x-2 w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors"
                          >
                            <Trash2 className="h-4 w-4" />
                            <span>Delete Client</span>
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </div>

              <div className="flex items-center justify-between mt-3 pt-3 border-t border-slate-100">
                <div className="text-sm text-slate-600">
                  <span className="font-medium">{client.totalBookings}</span> total bookings
                </div>
                <div className="text-sm text-slate-600">
                  Last visit: <span className="font-medium">{new Date(client.lastVisit).toLocaleDateString('en-ZA')}</span>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-4 sm:mt-6">
        <NewClientModal onClientAdded={handleAddClient} />
      </div>
    </div>
  );
};

export default ClientList;

