import React, { useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { useSearchServices } from '../hooks/useServices';
import SearchResults from '../components/SearchResults';
import Navigation from '../components/Navigation';
import BookingModal from '../components/BookingModal';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';

const Search = () => {
  const [searchParams] = useSearchParams();
  const query = searchParams.get('q') || '';
  const location = searchParams.get('location') || '';
  const category = searchParams.get('category') || '';
  const [selectedService, setSelectedService] = useState<any | null>(null);

  const { data, isLoading, error } = useSearchServices({
    name: query || undefined,
    city: location || undefined,
    category: category || undefined,
  });

  const handleBookingConfirm = () => {
    setSelectedService(null);
  };

  return (
    <div className="min-h-screen bg-background">
      <Navigation />
      
      <main className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <Link to="/">
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Button>
          </Link>
          
          <h1 className="text-3xl font-bold text-foreground mb-2">Search Results</h1>
          <p className="text-muted-foreground">
            {query && `Showing results for "${query}"`}
            {location && ` in ${location}`}
            {category && ` - ${category}`}
          </p>
        </div>

        {isLoading && (
          <div className="text-center py-12">
            <p className="text-muted-foreground">Loading search results...</p>
          </div>
        )}

        {error && (
          <div className="text-center py-12">
            <p className="text-destructive">Error loading search results. Please try again.</p>
          </div>
        )}

        {data && !isLoading && data.services.length > 0 && (
          <div>
            <p className="text-sm text-muted-foreground mb-4">
              Found {data.totalCount} {data.totalCount === 1 ? 'result' : 'results'}
            </p>
            <div className="grid gap-4">
              {data.services.map((service) => (
                <div key={service.id} className="bg-card rounded-lg p-6 border border-border hover:shadow-md transition-shadow">
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <h3 className="text-xl font-semibold text-foreground mb-2">{service.name}</h3>
                      <p className="text-muted-foreground mb-3">{service.description}</p>
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span>{service.location}</span>
                        {service.type && <span className="px-2 py-1 bg-secondary text-secondary-foreground rounded-full text-xs">{service.type}</span>}
                      </div>
                    </div>
                    <div className="text-right ml-6">
                      <div className="text-2xl font-bold text-foreground mb-2">${service.price}</div>
                      <Button onClick={() => setSelectedService(service)}>Book Now</Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {data && !isLoading && data.services.length === 0 && (
          <div className="text-center py-12">
            <h2 className="text-2xl font-semibold mb-2">No results found</h2>
            <p className="text-muted-foreground mb-4">
              Try adjusting your search terms or location
            </p>
            <Link to="/">
              <Button>Return to Home</Button>
            </Link>
          </div>
        )}
      </main>

      <BookingModal
        service={selectedService}
        isVisible={!!selectedService}
        onClose={() => setSelectedService(null)}
        onBookingConfirm={handleBookingConfirm}
      />
    </div>
  );
};

export default Search;
