import React, { useState, useRef, useEffect } from 'react';
import { Search, MapPin, X } from 'lucide-react';
import { useCitySuggestions } from '../hooks/useLocations';

interface UnifiedSearchProps {
  onSearch: (query: string, location: string) => void;
  placeholder?: string;
  className?: string;
  onInputChange?: (value: string) => void;
  currentValue?: string;
}

interface SearchSuggestion {
  type: 'service' | 'location';
  value: string;
  display: string;
}

const UnifiedSearch = ({ 
  onSearch, 
  placeholder = "Search for services or location...", 
  className = "",
  onInputChange,
  currentValue
}: UnifiedSearchProps) => {
  const [inputValue, setInputValue] = useState(currentValue || '');
  const [selectedService, setSelectedService] = useState('');
  const [selectedLocation, setSelectedLocation] = useState('');
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [activeSuggestionIndex, setActiveSuggestionIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);

  // Service types
  const serviceTypes = [
    'Barber', 'Hair Salon', 'Beauty Therapist', 'Massage Therapist',
    'Personal Trainer', 'Tutor', 'Plumber', 'Electrician', 'Cleaner',
    'Photographer', 'DJ', 'Caterer', 'Pet Groomer', 'Mechanic'
  ];

  // Get location suggestions
  const locationSuggestions = useCitySuggestions(inputValue);

  // Combine service and location suggestions
  const allSuggestions: SearchSuggestion[] = [
    // Service suggestions
    ...serviceTypes
      .filter(service => 
        service.toLowerCase().includes(inputValue.toLowerCase()) && 
        inputValue.length > 0
      )
      .slice(0, 5)
      .map(service => ({
        type: 'service' as const,
        value: service,
        display: service
      })),
    // Location suggestions
    ...locationSuggestions
      .slice(0, 5)
      .map(location => ({
        type: 'location' as const,
        value: location,
        display: location
      }))
  ];

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setInputValue(value);
    onInputChange?.(value);
    setShowSuggestions(value.length > 0);
    setActiveSuggestionIndex(-1);
  };

  // Sync with external current value
  useEffect(() => {
    if (currentValue !== undefined && currentValue !== inputValue) {
      setInputValue(currentValue);
    }
  }, [currentValue]);

  const handleSuggestionClick = (suggestion: SearchSuggestion) => {
    if (suggestion.type === 'service') {
      setSelectedService(suggestion.value);
      setInputValue(suggestion.value);
    } else {
      setSelectedLocation(suggestion.value);
      setInputValue(suggestion.value);
    }
    setShowSuggestions(false);
    inputRef.current?.focus();
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!showSuggestions || allSuggestions.length === 0) {
      if (e.key === 'Enter') {
        handleSearch();
      }
      return;
    }

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setActiveSuggestionIndex(prev => 
          prev < allSuggestions.length - 1 ? prev + 1 : 0
        );
        break;
      case 'ArrowUp':
        e.preventDefault();
        setActiveSuggestionIndex(prev => 
          prev > 0 ? prev - 1 : allSuggestions.length - 1
        );
        break;
      case 'Enter':
        e.preventDefault();
        if (activeSuggestionIndex >= 0 && allSuggestions[activeSuggestionIndex]) {
          handleSuggestionClick(allSuggestions[activeSuggestionIndex]);
        } else {
          handleSearch();
        }
        break;
      case 'Escape':
        setShowSuggestions(false);
        setActiveSuggestionIndex(-1);
        break;
    }
  };

  const handleSearch = () => {
    const query = inputValue.trim();
    if (!query) return;

    // Try to parse the input - if it contains a comma, split service and location
    const parts = query.split(',').map(part => part.trim());
    
    let serviceQuery = '';
    let locationQuery = '';

    if (parts.length === 2) {
      // Format: "service, location" or "location, service"
      const [first, second] = parts;
      
      // Check if first part is a known service
      const isFirstService = serviceTypes.some(service => 
        service.toLowerCase().includes(first.toLowerCase())
      );
      
      if (isFirstService) {
        serviceQuery = first;
        locationQuery = second;
      } else {
        // Assume first is location, second is service
        locationQuery = first;
        serviceQuery = second;
      }
    } else {
      // Single term - check if it's a known service or treat as general search
      const isKnownService = serviceTypes.some(service => 
        service.toLowerCase().includes(query.toLowerCase())
      );
      
      if (isKnownService || selectedService) {
        serviceQuery = selectedService || query;
        locationQuery = selectedLocation;
      } else {
        // Could be location or general search term
        serviceQuery = query;
        locationQuery = selectedLocation;
      }
    }

    onSearch(serviceQuery, locationQuery);
    setShowSuggestions(false);
  };

  const clearInput = () => {
    setInputValue('');
    setSelectedService('');
    setSelectedLocation('');
    setShowSuggestions(false);
    inputRef.current?.focus();
  };

  // Handle clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        suggestionsRef.current && 
        !suggestionsRef.current.contains(event.target as Node) &&
        !inputRef.current?.contains(event.target as Node)
      ) {
        setShowSuggestions(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className={`relative ${className}`}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-slate-400" />
        <input
          ref={inputRef}
          type="text"
          value={inputValue}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          onFocus={() => setShowSuggestions(inputValue.length > 0)}
          className="w-full pl-10 pr-12 py-4 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent text-lg"
          placeholder={placeholder}
        />
        {inputValue && (
          <button
            onClick={clearInput}
            className="absolute right-3 top-1/2 transform -translate-y-1/2 p-1 hover:bg-slate-100 rounded-full transition-colors"
          >
            <X className="h-4 w-4 text-slate-400" />
          </button>
        )}
      </div>

      {/* Selected filters display */}
      {(selectedService || selectedLocation) && (
        <div className="flex flex-wrap gap-2 mt-2">
          {selectedService && (
            <div className="flex items-center bg-blue-100 text-blue-800 px-3 py-1 rounded-full text-sm">
              <span>Service: {selectedService}</span>
              <button
                onClick={() => setSelectedService('')}
                className="ml-2 hover:bg-blue-200 rounded-full p-0.5"
              >
                <X className="h-3 w-3" />
              </button>
            </div>
          )}
          {selectedLocation && (
            <div className="flex items-center bg-green-100 text-green-800 px-3 py-1 rounded-full text-sm">
              <MapPin className="h-3 w-3 mr-1" />
              <span>{selectedLocation}</span>
              <button
                onClick={() => setSelectedLocation('')}
                className="ml-2 hover:bg-green-200 rounded-full p-0.5"
              >
                <X className="h-3 w-3" />
              </button>
            </div>
          )}
        </div>
      )}

      {/* Suggestions dropdown */}
      {showSuggestions && allSuggestions.length > 0 && (
        <div 
          ref={suggestionsRef}
          className="absolute z-10 w-full mt-1 bg-white border border-slate-200 rounded-xl shadow-lg max-h-60 overflow-y-auto"
        >
          {allSuggestions.map((suggestion, index) => (
            <button
              key={`${suggestion.type}-${suggestion.value}`}
              onClick={() => handleSuggestionClick(suggestion)}
              className={`w-full text-left px-4 py-3 hover:bg-slate-50 transition-colors border-b border-slate-100 last:border-b-0 first:rounded-t-xl last:rounded-b-xl ${
                index === activeSuggestionIndex ? 'bg-blue-50' : ''
              }`}
            >
              <div className="flex items-center space-x-3">
                {suggestion.type === 'service' ? (
                  <Search className="h-4 w-4 text-blue-500" />
                ) : (
                  <MapPin className="h-4 w-4 text-green-500" />
                )}
                <div>
                  <span className="text-slate-700">{suggestion.display}</span>
                  <div className="text-xs text-slate-500 capitalize">
                    {suggestion.type}
                  </div>
                </div>
              </div>
            </button>
          ))}
          
          <div className="px-4 py-2 bg-slate-50 border-t border-slate-200 text-xs text-slate-600 rounded-b-xl">
            💡 Tip: Try "Barber, Johannesburg" or just type what you're looking for
          </div>
        </div>
      )}
    </div>
  );
};

export default UnifiedSearch;