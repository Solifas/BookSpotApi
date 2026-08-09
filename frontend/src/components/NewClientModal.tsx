import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Plus } from 'lucide-react';
import { toast } from '@/components/ui/use-toast';

interface NewClientModalProps {
  onClientAdded?: (client: any) => void;
  children?: React.ReactNode;
}

const NewClientModal = ({ onClientAdded, children }: NewClientModalProps) => {
  const [open, setOpen] = useState(false);
  const [clientName, setClientName] = useState('');
  const [clientEmail, setClientEmail] = useState('');
  const [clientPhone, setClientPhone] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!clientName || !clientEmail) {
      toast({
        title: "Missing Information",
        description: "Please fill in the client name and email.",
        variant: "destructive",
      });
      return;
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(clientEmail)) {
      toast({
        title: "Invalid Email",
        description: "Please enter a valid email address.",
        variant: "destructive",
      });
      return;
    }

    setIsSubmitting(true);

    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      const newClient = {
        id: `client-${Date.now()}`,
        name: clientName,
        email: clientEmail,
        phone: clientPhone,
        totalBookings: 0,
        lastVisit: new Date().toISOString().split('T')[0]
      };

      // Call the callback if provided
      if (onClientAdded) {
        onClientAdded(newClient);
      }
      
      toast({
        title: "Client Added",
        description: `${clientName} has been successfully added to your client list.`,
      });

      // Reset form
      setClientName('');
      setClientEmail('');
      setClientPhone('');
      setOpen(false);
    } catch (error) {
      toast({
        title: "Error",
        description: "Failed to add client. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        {children || (
          <Button className="w-full bg-gradient-to-r from-blue-500 to-green-500 text-white py-3 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl text-sm sm:text-base">
            <Plus className="h-4 w-4 mr-2" />
            Add New Client
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold">Add New Client</DialogTitle>
        </DialogHeader>
        
        <form onSubmit={handleSubmit} className="space-y-4 sm:space-y-6">
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
            <Label htmlFor="clientEmail">Email Address *</Label>
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

          <div className="flex gap-3 pt-4">
            <Button 
              type="button" 
              variant="outline" 
              onClick={() => setOpen(false)}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button 
              type="submit" 
              className="flex-1 bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Adding...' : 'Add Client'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default NewClientModal;