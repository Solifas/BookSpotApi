// Example component showing how to use the new hooks
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSearchServices } from '../hooks/useServices';
import { ServiceSearchParams } from '../types/api';
import { Service } from '../data/servicesData';
import { Loader2, Search, MapPin, Filter, X } from 'lucide-react';

const ServiceList: React.FC = () => {
    const [searchQuery, setSearchQuery] = useState('');
    const [categoryFilter, setCategoryFilter] = useState('');
    const [searchParams, setSearchParams] = useState<ServiceSearchParams>({
        page: 1,
        pageSize: 10
    });

    // Use the custom hook for searching services
    const {
        data: searchResults,
        isLoading,
        error,
        refetch
    } = useSearchServices(searchParams);

    const handleSearch = (e: React.FormEvent) => {
        e.preventDefault();
        setSearchParams(prev => ({
            ...prev,
            name: searchQuery,
            category: categoryFilter,
            page: 1
        }));
    };

    const handleSearchInputChange = (value: string) => {
        setSearchQuery(value);
        // Auto-search as user types
        setSearchParams(prev => ({
            ...prev,
            name: value,
            page: 1
        }));
    };

    const handleCategoryChange = (category: string) => {
        setCategoryFilter(category);
        setSearchParams(prev => ({
            ...prev,
            category,
            page: 1
        }));
    };

    const clearSearchQuery = () => {
        setSearchQuery('');
        setSearchParams(prev => ({
            ...prev,
            name: undefined,
            page: 1
        }));
    };

    const clearCategoryFilter = () => {
        setCategoryFilter('');
        setSearchParams(prev => ({
            ...prev,
            category: undefined,
            page: 1
        }));
    };

    if (isLoading) {
        return (
            <div className="flex items-center justify-center p-8">
                <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
                <span className="ml-2 text-slate-600">Searching services...</span>
            </div>
        );
    }

    if (error) {
        return (
            <div className="text-center p-8">
                <p className="text-red-600 mb-4">Failed to load services</p>
                <button
                    onClick={() => refetch()}
                    className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
                >
                    Try Again
                </button>
            </div>
        );
    }

    const services = searchResults?.services || [];
    const totalCount = searchResults?.totalCount || 0;
    const totalPages = searchResults?.totalPages || 0;

    const handlePageChange = (page: number) => {
        setSearchParams(prev => ({
            ...prev,
            page
        }));
    };

    return (
        <div className="space-y-6">
            {/* Search and Filters */}
            <div className="bg-white rounded-lg shadow-sm border p-6">
                <form onSubmit={handleSearch}>
                    <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
                        {/* Search Input - Takes 3 columns */}
                        <div className="relative md:col-span-3">
                            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-slate-400" />
                            <input
                                type="text"
                                placeholder="Search services... (e.g., haircut, massage therapy, home cleaning)"
                                value={searchQuery}
                                className="w-full pl-11 pr-10 py-3 text-base border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
                                onChange={(e) => handleSearchInputChange(e.target.value)}
                                minLength={1}
                                maxLength={200}
                            />
                            {searchQuery && (
                                <button
                                    type="button"
                                    onClick={clearSearchQuery}
                                    className="absolute right-3 top-1/2 transform -translate-y-1/2 p-1 hover:bg-slate-100 rounded-full transition-colors"
                                    aria-label="Clear search"
                                >
                                    <X className="h-4 w-4 text-slate-400" />
                                </button>
                            )}
                            {searchQuery && (
                                <div className="absolute left-0 -bottom-6 text-xs text-slate-500">
                                    Searching for: "{searchQuery}"
                                </div>
                            )}
                        </div>

                        {/* Category Filter - Takes 2 columns */}
                        <div className="relative md:col-span-2">
                            <Filter className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
                            <select
                                value={categoryFilter}
                                className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent appearance-none"
                                onChange={(e) => handleCategoryChange(e.target.value)}
                            >
                                <option value="">All Categories</option>
                                <option value="Hair Salon">Hair Salon</option>
                                <option value="Massage Therapy">Massage Therapy</option>
                                <option value="Beauty">Beauty</option>
                                <option value="Fitness">Fitness</option>
                                <option value="Education">Education</option>
                                <option value="Home Services">Home Services</option>
                            </select>
                        </div>
                    </div>
                </form>

                {/* Active Filters Display */}
                {(searchQuery || categoryFilter) && (
                    <div className="flex flex-wrap gap-2 mt-4 pt-4 border-t border-slate-200">
                        <span className="text-sm text-slate-600">Active filters:</span>
                        {searchQuery && (
                            <div className="flex items-center bg-blue-100 text-blue-800 px-3 py-1 rounded-full text-sm">
                                <Search className="h-3 w-3 mr-1" />
                                <span>{searchQuery}</span>
                                <button
                                    onClick={clearSearchQuery}
                                    className="ml-2 hover:bg-blue-200 rounded-full p-0.5"
                                >
                                    <X className="h-3 w-3" />
                                </button>
                            </div>
                        )}
                        {categoryFilter && (
                            <div className="flex items-center bg-purple-100 text-purple-800 px-3 py-1 rounded-full text-sm">
                                <Filter className="h-3 w-3 mr-1" />
                                <span>{categoryFilter}</span>
                                <button
                                    onClick={clearCategoryFilter}
                                    className="ml-2 hover:bg-purple-200 rounded-full p-0.5"
                                >
                                    <X className="h-3 w-3" />
                                </button>
                            </div>
                        )}
                    </div>
                )}
            </div>

            {/* Results Summary */}
            <div className="flex justify-between items-center">
                <p className="text-slate-600">
                    Found {totalCount} services
                </p>
                <p className="text-sm text-slate-500">
                    Page {searchParams.page} of {totalPages}
                </p>
            </div>

            {/* Service Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {services.map((service) => (
                    <ServiceCard key={service.id} service={service} />
                ))}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex justify-center space-x-2">
                    <button
                        onClick={() => handlePageChange(searchParams.page! - 1)}
                        disabled={searchParams.page === 1}
                        className="px-4 py-2 border border-slate-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-50"
                    >
                        Previous
                    </button>

                    {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                        const page = i + 1;
                        return (
                            <button
                                key={page}
                                onClick={() => handlePageChange(page)}
                                className={`px-4 py-2 border rounded-lg ${searchParams.page === page
                                        ? 'bg-blue-600 text-white border-blue-600'
                                        : 'border-slate-300 hover:bg-slate-50'
                                    }`}
                            >
                                {page}
                            </button>
                        );
                    })}

                    <button
                        onClick={() => handlePageChange(searchParams.page! + 1)}
                        disabled={searchParams.page === totalPages}
                        className="px-4 py-2 border border-slate-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-50"
                    >
                        Next
                    </button>
                </div>
            )}
        </div>
    );
};

// Service Card Component
const ServiceCard: React.FC<{ service: Service }> = ({ service }) => {
    const navigate = useNavigate();
    
    const handleBookNow = () => {
        navigate(`/book/${service.businessId || service.id}`);
    };
    
    return (
        <div className="bg-white rounded-lg shadow-sm border hover:shadow-md transition-shadow">
            <img
                src={service.image}
                alt={service.name}
                className="w-full h-48 object-cover rounded-t-lg"
            />
            <div className="p-4">
                <h3 className="font-semibold text-lg text-slate-900 mb-2">{service.name}</h3>
                <p className="text-slate-600 text-sm mb-3">{service.description}</p>

                <div className="flex justify-between items-center mb-3">
                    <span className="text-2xl font-bold text-slate-900">{service.price}</span>
                    <div className="flex items-center">
                        <span className="text-yellow-500">★</span>
                        <span className="text-sm text-slate-600 ml-1">
                            {service.rating} ({service.reviewCount})
                        </span>
                    </div>
                </div>

                <div className="flex justify-between items-center text-sm text-slate-500 mb-4">
                    <span>{service.location}</span>
                    <span className={`px-2 py-1 rounded-full text-xs ${service.availability === 'Available'
                            ? 'bg-green-100 text-green-800'
                            : 'bg-red-100 text-red-800'
                        }`}>
                        {service.availability}
                    </span>
                </div>

                <button 
                    onClick={handleBookNow}
                    className="w-full bg-blue-600 text-white py-2 rounded-lg hover:bg-blue-700 transition-colors"
                >
                    Book Now
                </button>
            </div>
        </div>
    );
};

export default ServiceList;