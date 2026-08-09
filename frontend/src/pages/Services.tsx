import React from 'react';
import { useNavigate } from 'react-router-dom';
import Navigation from '../components/Navigation';
import ServiceList from '../components/ServiceList';
import { ArrowLeft } from 'lucide-react';

const Services = () => {
    const navigate = useNavigate();

    return (
        <div className="min-h-screen bg-slate-50">
            <Navigation />
            
            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                {/* Header */}
                <div className="mb-8">
                    <button
                        onClick={() => navigate('/')}
                        className="inline-flex items-center space-x-2 text-slate-600 hover:text-slate-900 mb-4"
                    >
                        <ArrowLeft className="h-5 w-5" />
                        <span>Back to Home</span>
                    </button>
                    
                    <h1 className="text-3xl md:text-4xl font-bold text-slate-900 mb-2">
                        All Services
                    </h1>
                    <p className="text-lg text-slate-600">
                        Browse and search through all available services
                    </p>
                </div>

                {/* Service List Component */}
                <ServiceList />
            </main>
        </div>
    );
};

export default Services;
