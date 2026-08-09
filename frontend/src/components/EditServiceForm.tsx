import React, { useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { useForm } from 'react-hook-form';

export interface EditableService {
  id: string;
  name: string;
  duration: number;
  price: number;
  description: string;
  active: boolean;
  location?: string;
  category?: string;
  imageUrl?: string;
  tags?: string[];
}

interface EditServiceFormProps {
  open: boolean;
  service: EditableService | null;
  onOpenChange: (open: boolean) => void;
  onUpdateService: (service: EditableService) => Promise<void>;
}

interface FormData {
  name: string;
  location?: string;
  duration: number;
  price: number;
  description: string;
  active: boolean;
}

const EditServiceForm: React.FC<EditServiceFormProps> = ({ open, service, onOpenChange, onUpdateService }) => {
  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors, isSubmitting }
  } = useForm<FormData>({
    defaultValues: {
      name: '',
      location: '',
      duration: 30,
      price: 0,
      description: '',
      active: true,
    }
  });

  const activeValue = watch('active');

  useEffect(() => {
    if (service) {
      reset({
        name: service.name,
        location: service.location || '',
        duration: service.duration,
        price: service.price,
        description: service.description,
        active: service.active,
      });
    }
  }, [service, reset]);

  const handleDialogChange = (nextOpen: boolean) => {
    onOpenChange(nextOpen);
  };

  const onSubmit = async (data: FormData) => {
    if (!service) return;

    const updatedService: EditableService = {
      ...service,
      name: data.name,
      duration: data.duration,
      price: data.price,
      description: data.description,
      active: data.active,
      location: data.location ?? service.location,
    };

    await onUpdateService(updatedService);
  };

  return (
    <Dialog open={open} onOpenChange={handleDialogChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Edit Service</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-name">Service Name</Label>
            <Input
              id="edit-name"
              {...register('name', { required: 'Service name is required' })}
              placeholder="e.g., Classic Haircut"
            />
            {errors.name && (
              <p className="text-sm text-red-600">{errors.name.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-location">Location</Label>
            <Input
              id="edit-location"
              {...register('location')}
              placeholder="e.g., Sandton, Johannesburg"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="edit-duration">Duration (minutes)</Label>
              <Input
                id="edit-duration"
                type="number"
                {...register('duration', {
                  required: 'Duration is required',
                  min: { value: 1, message: 'Duration must be at least 1 minute' },
                })}
              />
              {errors.duration && (
                <p className="text-sm text-red-600">{errors.duration.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-price">Price ($)</Label>
              <Input
                id="edit-price"
                type="number"
                step="0.01"
                {...register('price', {
                  required: 'Price is required',
                  min: { value: 0, message: 'Price must be positive' },
                })}
              />
              {errors.price && (
                <p className="text-sm text-red-600">{errors.price.message}</p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-description">Description</Label>
            <Textarea
              id="edit-description"
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
              id="edit-active"
              checked={activeValue}
              onCheckedChange={(checked) => setValue('active', checked)}
            />
            <Label htmlFor="edit-active">Active service</Label>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button
              type="submit"
              className="bg-gradient-to-r from-blue-500 to-green-500 hover:from-blue-600 hover:to-green-600"
              disabled={isSubmitting}
            >
              Save Changes
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default EditServiceForm;
