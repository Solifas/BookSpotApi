import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { format } from 'date-fns';
import { CalendarIcon, Plus } from 'lucide-react';
import { toast } from '@/components/ui/use-toast';
import { useAuth } from '@/contexts/AuthContext';

const services = [
  { id: '1', name: "Premium Men's Haircut", price: 150 },
  { id: '2', name: 'Ladies Hair Styling', price: 280 },
  { id: '3', name: 'Relaxing Facial Treatment', price: 320 },
  { id: '4', name: 'Deep Tissue Massage', price: 450 },
];

const timeSlots = [
  '09:00', '09:30', '10:00', '10:30', '11:00', '11:30',
  '12:00', '12:30', '13:00', '13:30', '14:00', '14:30',
  '15:00', '15:30', '16:00', '16:30', '17:00', '17:30'
];

interface NewBookingModalProps {
  children?: React.ReactNode;
}

const NewBookingModal = ({ children }: NewBookingModalProps) => {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [date, setDate] = useState<Date>();
  const [clientName, setClientName] = useState('');
  const [clientEmail, setClientEmail] = useState('');
  const [clientPhone, setClientPhone] = useState('');
  const [selectedService, setSelectedService] = useState('');
  const [selectedTime, setSelectedTime] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { user, isLoggedIn } = useAuth();

  const handleOpenChange = (newOpen: boolean) => {
    if (newOpen && !isLoggedIn) {
      navigate('/login');
      return;
    }
    setOpen(newOpen);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!date || !selectedTime || !selectedService || !clientName || !clientEmail) {
      toast({
        title: "Missing Information",
        description: "Please fill in all required fields.",
        variant: "destructive",
      });
      return;
    }

    setIsSubmitting(true);

    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      toast({
        title: "Booking Created",
        description: `New booking created for ${clientName} on ${format(date, 'PPP')} at ${selectedTime}.`,
      });

      // Reset form
      setDate(undefined);
      setClientName('');
      setClientEmail('');
      setClientPhone('');
      setSelectedService('');
      setSelectedTime('');
      setOpen(false);
    } catch (error) {
      toast({
        title: "Error",
        description: "Failed to create booking. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedServiceDetails = services.find(s => s.id === selectedService);

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        {children || (
          <Button className="w-full bg-gradient-to-r from-blue-500 to-green-500 text-white p-3 sm:p-4 rounded-lg sm:rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl text-sm sm:text-base">
            <Plus className="h-4 w-4 mr-2" />
            New Booking
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className="sm:max-w-[500px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold">Create New Booking</DialogTitle>
        </DialogHeader>
        
        <form onSubmit={handleSubmit} className="space-y-4 sm:space-y-6">
          {/* Client Information */}
          <div className="space-y-4">
            <h3 className="font-semibold text-slate-900">Client Information</h3>
            
            <div className="space-y-2">
              <Label htmlFor="clientName">Full Name *</Label>
              <Input
                id="clientName"
                value={clientName}
                onChange={(e) => setClientName(e.target.value)}
                placeholder="Enter client's full name"
                required
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="clientEmail">Email *</Label>
              <Input
                id="clientEmail"
                type="email"
                value={clientEmail}
                onChange={(e) => setClientEmail(e.target.value)}
                placeholder="Enter client's email"
                required
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="clientPhone">Phone Number</Label>
              <Input
                id="clientPhone"
                value={clientPhone}
                onChange={(e) => setClientPhone(e.target.value)}
                placeholder="Enter client's phone number"
              />
            </div>
          </div>

          {/* Service Selection */}
          <div className="space-y-2">
            <Label>Service *</Label>
            <Select value={selectedService} onValueChange={setSelectedService}>
              <SelectTrigger>
                <SelectValue placeholder="Select a service" />
              </SelectTrigger>
              <SelectContent>
                {services.map((service) => (
                  <SelectItem key={service.id} value={service.id}>
                    <div className="flex justify-between w-full">
                      <span>{service.name}</span>
                      <span className="text-slate-600 ml-2">R{service.price}</span>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Date Selection */}
          <div className="space-y-2">
            <Label>Date *</Label>
            <Popover>
              <PopoverTrigger asChild>
                <Button
                  variant="outline"
                  className="w-full justify-start text-left font-normal"
                >
                  <CalendarIcon className="mr-2 h-4 w-4" />
                  {date ? format(date, "PPP") : "Pick a date"}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <Calendar
                  mode="single"
                  selected={date}
                  onSelect={setDate}
                  disabled={(date) => date < new Date() || date < new Date("1900-01-01")}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
          </div>

          {/* Time Selection */}
          <div className="space-y-2">
            <Label>Time *</Label>
            <Select value={selectedTime} onValueChange={setSelectedTime}>
              <SelectTrigger>
                <SelectValue placeholder="Select a time" />
              </SelectTrigger>
              <SelectContent>
                {timeSlots.map((time) => (
                  <SelectItem key={time} value={time}>
                    {time}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Service Details */}
          {selectedServiceDetails && (
            <div className="bg-slate-50 p-4 rounded-lg">
              <h4 className="font-medium text-slate-900 mb-2">Service Details</h4>
              <div className="flex justify-between text-sm">
                <span>{selectedServiceDetails.name}</span>
                <span className="font-semibold">R{selectedServiceDetails.price}</span>
              </div>
            </div>
          )}

          {/* Submit Button */}
          <Button 
            type="submit" 
            className="w-full bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Creating Booking...' : 'Create Booking'}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default NewBookingModal;