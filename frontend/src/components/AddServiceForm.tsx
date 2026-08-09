
import React from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { useForm } from 'react-hook-form';
import { toast } from '@/components/ui/use-toast';

interface Service {
  id: string;
  name: string;
  duration: number;
  price: number;
  description: string;
  active: boolean;
}

interface AddServiceFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onAddService: (service: Service) => void;
}

interface FormData {
  name: string;
  duration: number;
  price: number;
  description: string;
  active: boolean;
}

const AddServiceForm = ({ open, onOpenChange, onAddService }: AddServiceFormProps) => {
  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors }
  } = useForm<FormData>({
    defaultValues: {
      name: '',
      duration: 30,
      price: 0,
      description: '',
      active: true
    }
  });

  const activeValue = watch('active');

  const onSubmit = (data: FormData) => {
    const newService: Service = {
      id: Date.now().toString(),
      name: data.name,
      duration: data.duration,
      price: data.price,
      description: data.description,
      active: data.active
    };

    onAddService(newService);

    toast({
      title: "Service Added",
      description: `${data.name} has been added to your services.`,
    });

    reset();
    onOpenChange(false);
  };

  const handleClose = () => {
    reset();
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Add New Service</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">Service Name</Label>
            <Input
              id="name"
              {...register('name', { required: 'Service name is required' })}
              placeholder="e.g., Classic Haircut"
            />
            {errors.name && (
              <p className="text-sm text-red-600">{errors.name.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="duration">Duration (minutes)</Label>
              <Input
                id="duration"
                type="number"
                {...register('duration', {
                  required: 'Duration is required',
                  min: { value: 1, message: 'Duration must be at least 1 minute' }
                })}
                placeholder="30"
              />
              {errors.duration && (
                <p className="text-sm text-red-600">{errors.duration.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="price">Price (R)</Label>
              <Input
                id="price"
                type="number"
                step="0.01"
                {...register('price', {
                  required: 'Price is required',
                  min: { value: 0, message: 'Price must be positive' }
                })}
                placeholder="35.00"
              />
              {errors.price && (
                <p className="text-sm text-red-600">{errors.price.message}</p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              {...register('description', { required: 'Description is required' })}
              placeholder="Brief description of the service"
              rows={3}
            />
            {errors.description && (
              <p className="text-sm text-red-600">{errors.description.message}</p>
            )}
          </div>

          <div className="flex items-center space-x-2">
            <Switch
              id="active"
              checked={activeValue}
              onCheckedChange={(checked) => setValue('active', checked)}
            />
            <Label htmlFor="active">Active service</Label>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={handleClose}>
              Cancel
            </Button>
            <Button type="submit" className="bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600">
              Add Service
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default AddServiceForm;
