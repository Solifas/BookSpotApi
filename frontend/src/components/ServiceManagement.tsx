import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Plus, Edit, Trash2, Clock, DollarSign, Loader2 } from 'lucide-react';
import { toast } from '@/components/ui/use-toast';
import AddServiceForm from './AddServiceForm';
import EditServiceForm from './EditServiceForm';
import { DataSourceAdapter } from '../services/dataSourceAdapter';
import { Service, UpdateServiceCommand } from '../types/api';
import { isMockMode } from '../config/dataSource';
import { useAuth } from '../contexts/AuthContext';

interface LocalService {
  id: string;
  name: string;
  duration: number;
  price: number;
  description: string;
  active: boolean;
  category?: string;
  imageUrl?: string;
  tags?: string[];
  location?: string;
}

const mapApiServiceToLocal = (service: Service): LocalService => ({
  id: service.id,
  name: service.name,
  duration: service.durationMinutes,
  price: service.price,
  description: service.description ?? '',
  active: service.isActive,
  category: service.category,
  imageUrl: service.imageUrl,
  tags: service.tags,
});

const ServiceManagement = () => {
  const { user, isLoggedIn } = useAuth();
  const [isAddFormOpen, setIsAddFormOpen] = useState(false);
  const [isEditFormOpen, setIsEditFormOpen] = useState(false);
  const [serviceBeingEdited, setServiceBeingEdited] = useState<LocalService | null>(null);
  const [services, setServices] = useState<LocalService[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Mock services - only shown in mock mode
  const mockServices: LocalService[] = [
    {
      id: '1',
      name: 'Classic Haircut',
      duration: 30,
      price: 35,
      description: 'Professional men\'s haircut with styling',
      active: true
    },
    {
      id: '2',
      name: 'Beard Trim & Shape',
      duration: 20,
      price: 25,
      description: 'Beard trimming and shaping service',
      active: true
    },
    {
      id: '3',
      name: 'Cut & Beard Combo',
      duration: 45,
      price: 55,
      description: 'Complete haircut and beard service',
      active: true
    },
    {
      id: '4',
      name: 'Hair Styling',
      duration: 25,
      price: 30,
      description: 'Hair styling and finishing',
      active: false
    }
  ];

  // Load services based on data source mode
  useEffect(() => {
    const loadServices = async () => {
      setLoading(true);
      setError(null);

      try {
        if (isMockMode()) {
          // In mock mode, use the hardcoded services
          console.log('🎭 ServiceManagement: Using mock services');
          setServices(mockServices);
        } else {
          // In API mode, fetch from the backend
          if (!isLoggedIn || !user) {
            throw new Error('User must be logged in to access services');
          }

          if (user.type !== 'provider') {
            throw new Error('Only providers can manage services');
          }

          console.log('🌐 ServiceManagement: Fetching services from API for user:', user.id);
          const response = await DataSourceAdapter.getBusinessServices(user.id);

          if (response.error) {
            throw new Error(response.error);
          }

          // Convert API services to local format
          const apiServices: LocalService[] = (response.data || []).map(mapApiServiceToLocal);
          console.log(apiServices);
          setServices(apiServices);
        }
      } catch (err) {
        console.error('Failed to load services:', err);
        setError(err instanceof Error ? err.message : 'Failed to load services');

        // Fallback to empty array in API mode, mock services in mock mode
        if (isMockMode()) {
          setServices(mockServices);
        } else {
          setServices([]);
        }
      } finally {
        setLoading(false);
      }
    };

    loadServices();
  }, [isLoggedIn, user]);

  const handleEditFormOpenChange = (open: boolean) => {
    setIsEditFormOpen(open);
    if (!open) {
      setServiceBeingEdited(null);
    }
  };

  const handleEditServiceClick = (service: LocalService) => {
    setServiceBeingEdited(service);
    setIsEditFormOpen(true);
  };

  const handleAddService = async (newService: LocalService) => {
    try {
      if (isMockMode()) {
        // In mock mode, just add to local state
        setServices(prev => [...prev, newService]);
        toast({
          title: "Service Added",
          description: `${newService.name} has been added to your services.`,
        });
      } else {
        // In API mode, create via API
        if (!user) {
          throw new Error('User must be logged in to create services');
        }

        const response = await DataSourceAdapter.createService({
          businessId: user.id,
          name: newService.name,
          description: newService.description,
          category: newService.category,
          price: newService.price,
          durationMinutes: newService.duration,
          imageUrl: newService.imageUrl,
          tags: newService.tags,
          isActive: newService.active
        });

        if (response.error) {
          throw new Error(response.error);
        }

        const createdService = response.data ? mapApiServiceToLocal(response.data) : newService;

        setServices(prev => [...prev, createdService]);
        toast({
          title: "Service Added",
          description: `${createdService.name} has been added to your services.`,
        });
      }
    } catch (err) {
      console.error('Failed to add service:', err);
      toast({
        title: "Error",
        description: "Failed to add service. Please try again.",
        variant: "destructive",
      });
    }
  };

  const handleUpdateService = async (updatedService: LocalService): Promise<void> => {
    try {
      if (isMockMode()) {
        setServices(prev => prev.map(s => (s.id === updatedService.id ? updatedService : s)));
        toast({
          title: 'Service Updated',
          description: `${updatedService.name} has been updated.`,
        });
        handleEditFormOpenChange(false);
        return;
      }

      const updatePayload: UpdateServiceCommand = {
        id: updatedService.id,
        name: updatedService.name,
        description: updatedService.description,
        price: updatedService.price,
        durationMinutes: updatedService.duration,
        isActive: updatedService.active,
        category: updatedService.category,
        imageUrl: updatedService.imageUrl,
        tags: updatedService.tags,
        location: updatedService.location,
      };

      const response = await DataSourceAdapter.updateService(updatedService.id, updatePayload);

      if (response.error) {
        throw new Error(response.error);
      }

      if (!response.data) {
        throw new Error('Service not found');
      }

      const syncedService = mapApiServiceToLocal(response.data);
      setServices(prev => prev.map(s => (s.id === syncedService.id ? syncedService : s)));

      toast({
        title: 'Service Updated',
        description: `${syncedService.name} has been updated.`,
      });
      handleEditFormOpenChange(false);
    } catch (err) {
      console.error('Failed to update service:', err);
      toast({
        title: 'Error',
        description: 'Failed to update service. Please try again.',
        variant: 'destructive',
      });
    }
  };

  const handleToggleService = async (service: LocalService) => {
    const nextService: LocalService = {
      ...service,
      active: !service.active,
    };

    const updatePayload: UpdateServiceCommand = {
      id: service.id,
      name: service.name,
      description: service.description,
      price: service.price,
      durationMinutes: service.duration,
      isActive: nextService.active,
      category: service.category,
      imageUrl: service.imageUrl,
      tags: service.tags,
    };

    try {
      if (isMockMode()) {
        setServices(prev => prev.map(s => (s.id === service.id ? nextService : s)));
      } else {
        const response = await DataSourceAdapter.updateService(service.id, updatePayload);

        if (response.error) {
          throw new Error(response.error);
        }

        if (response.data === null) {
          throw new Error('Service not found');
        }

        const syncedService = mapApiServiceToLocal(response.data);
        setServices(prev => prev.map(s => (s.id === service.id ? syncedService : s)));
      }

      toast({
        title: nextService.active ? "Service Enabled" : "Service Disabled",
        description: `${nextService.name} is now ${nextService.active ? 'active' : 'inactive'} - duration ${nextService.duration} min - price $${nextService.price}`,
      });
    } catch (err) {
      console.error('Failed to update service:', err);
      toast({
        title: "Error",
        description: "Failed to update service. Please try again.",
        variant: "destructive",
      });
    }
  };

  const handleDeleteService = async (serviceId: string) => {
    const service = services.find(s => s.id === serviceId);
    if (!service) return;

    try {
      if (isMockMode()) {
        // In mock mode, just remove from local state
        setServices(prev => prev.filter(s => s.id !== serviceId));
      } else {
        // In API mode, delete via API
        const response = await DataSourceAdapter.deleteService(serviceId);

        if (response.error) {
          throw new Error(response.error);
        }

        // Remove from local state
        setServices(prev => prev.filter(s => s.id !== serviceId));
      }

      toast({
        title: "Service Deleted",
        description: `${service.name} has been removed from your services.`,
        variant: "destructive",
      });
    } catch (err) {
      console.error('Failed to delete service:', err);
      toast({
        title: "Error",
        description: "Failed to delete service. Please try again.",
        variant: "destructive",
      });
    }
  };

  // Show authentication required state
  if (!isMockMode() && !isLoggedIn) {
    return (
      <div className="space-y-6">
        <div className="flex justify-between items-center">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">Service Management</h2>
            <p className="text-slate-600">Manage your services, pricing, and availability</p>
          </div>
        </div>
        <Card className="p-12">
          <div className="text-center">
            <h3 className="text-lg font-semibold text-slate-900 mb-2">Authentication Required</h3>
            <p className="text-slate-600 mb-4">
              Please log in as a provider to manage your services.
            </p>
          </div>
        </Card>
      </div>
    );
  }

  // Show provider access required state
  if (!isMockMode() && isLoggedIn && user?.type !== 'provider') {
    return (
      <div className="space-y-6">
        <div className="flex justify-between items-center">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">Service Management</h2>
            <p className="text-slate-600">Manage your services, pricing, and availability</p>
          </div>
        </div>
        <Card className="p-12">
          <div className="text-center">
            <h3 className="text-lg font-semibold text-slate-900 mb-2">Provider Access Required</h3>
            <p className="text-slate-600 mb-4">
              Only service providers can manage services. Please register as a provider to access this feature.
            </p>
          </div>
        </Card>
      </div>
    );
  }

  // Show loading state
  if (loading) {
    return (
      <div className="space-y-6">
        <div className="flex justify-between items-center">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">Service Management</h2>
            <p className="text-slate-600">Manage your services, pricing, and availability</p>
          </div>
        </div>
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
          <span className="ml-2 text-slate-600">Loading services...</span>
        </div>
      </div>
    );
  }

  // Show error state
  if (error && !isMockMode()) {
    return (
      <div className="space-y-6">
        <div className="flex justify-between items-center">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">Service Management</h2>
            <p className="text-slate-600">Manage your services, pricing, and availability</p>
          </div>
        </div>
        <Card className="p-6">
          <div className="text-center">
            <p className="text-red-600 mb-4">Failed to load services: {error}</p>
            <Button onClick={() => window.location.reload()}>
              Try Again
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Service Management</h2>
          <p className="text-slate-600">
            Manage your services, pricing, and availability
            {isMockMode() && (
              <Badge variant="outline" className="ml-2">
                Mock Mode
              </Badge>
            )}
          </p>
        </div>
        <Button
          onClick={() => setIsAddFormOpen(true)}
          className="bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add New Service
        </Button>
      </div>

      {/* Empty state */}
      {services.length === 0 && !loading && (
        <Card className="p-12">
          <div className="text-center">
            <h3 className="text-lg font-semibold text-slate-900 mb-2">No services yet</h3>
            <p className="text-slate-600 mb-4">
              {isMockMode()
                ? "Switch to mock mode to see sample services, or add your first service."
                : "Get started by adding your first service."
              }
            </p>
            <Button
              onClick={() => setIsAddFormOpen(true)}
              className="bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600"
            >
              <Plus className="h-4 w-4 mr-2" />
              Add Your First Service
            </Button>
          </div>
        </Card>
      )}

      {/* Services Grid */}
      {services.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {services.map((service) => (
            <Card key={service.id} className={`${!service.active ? 'opacity-60' : ''}`}>
              <CardHeader className="pb-3">
                <div className="flex justify-between items-start">
                  <CardTitle className="text-lg">{service.name}</CardTitle>
                  <Badge variant={service.active ? "default" : "secondary"}>
                    {service.active ? 'Active' : 'Inactive'}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <p className="text-sm text-slate-600">{service.description}</p>

                <div className="flex justify-between items-center">
                  <div className="flex items-center gap-1 text-sm text-slate-600">
                    <Clock className="h-3 w-3" />
                    {service.duration} min
                  </div>
                  <div className="flex items-center gap-1 text-lg font-semibold text-slate-900">
                    R{service.price}
                  </div>
                </div>

                <div className="flex gap-2 pt-2">
                  <Button
                    variant="outline"
                    size="sm"
                    className="flex-1"
                    onClick={() => handleToggleService(service)}
                  >
                    {service.active ? 'Disable' : 'Enable'}
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleEditServiceClick(service)}
                  >
                    <Edit className="h-3 w-3" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleDeleteService(service.id)}
                    className="text-red-600 hover:text-red-700"
                  >
                    <Trash2 className="h-3 w-3" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Quick Stats */}
      {services.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-8">
          <Card>
            <CardContent className="p-6">
              <div className="text-center">
                <div className="text-2xl font-bold text-slate-900">{services.filter(s => s.active).length}</div>
                <div className="text-sm text-slate-600">Active Services</div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="text-center">
                <div className="text-2xl font-bold text-slate-900">
                  {services.filter(s => s.active).length > 0
                    ? `$${Math.round(services.filter(s => s.active).reduce((acc, s) => acc + s.price, 0) / services.filter(s => s.active).length)}`
                    : '$0'
                  }
                </div>
                <div className="text-sm text-slate-600">Average Price</div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="text-center">
                <div className="text-2xl font-bold text-slate-900">
                  {services.filter(s => s.active).length > 0
                    ? `${Math.round(services.filter(s => s.active).reduce((acc, s) => acc + s.duration, 0) / services.filter(s => s.active).length)} min`
                    : '0 min'
                  }
                </div>
                <div className="text-sm text-slate-600">Average Duration</div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Add Service Form */}
      <AddServiceForm
        open={isAddFormOpen}
        onOpenChange={setIsAddFormOpen}
        onAddService={handleAddService}
      />
      <EditServiceForm
        open={isEditFormOpen}
        service={serviceBeingEdited}
        onOpenChange={handleEditFormOpenChange}
        onUpdateService={handleUpdateService}
      />
    </div>
  );
};

export default ServiceManagement;
