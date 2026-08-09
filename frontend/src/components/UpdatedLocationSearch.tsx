// Updated LocationSearch component - Example of replacing mock data with TanStack Query
import React, { useState } from 'react';
import { MapPin, Search } from 'lucide-react';
import { useCitySuggestions } from '../hooks/useLocations';

interface LocationSearchProps {
    value: string;
    onChange: (location: string) => void;
    placeholder?: string;
}

const UpdatedLocationSearch = ({ value, onChange, placeholder = "Search location..." }: LocationSearchProps) => {
    const [showSuggestions, setShowSuggestions] = useState(false);

    // Replace mock data with real API call
    const suggestions = useCitySuggestions(value);

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const inputValue = e.target.value;
        onChange(inputValue);
        setShowSuggestions(inputValue.length > 0);
    };

    const handleSuggestionClick = (suggestion: string) => {
        onChange(suggestion);
        setShowSuggestions(false);
    };

    return null;
};

export default UpdatedLocationSearch;