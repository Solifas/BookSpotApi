
import React from 'react';
import { Check } from 'lucide-react';

interface PricingCardProps {
  title: string;
  price: string;
  period: string;
  features: string[];
  isPopular?: boolean;
  ctaText: string;
}

const PricingCard = ({ title, price, period, features, isPopular = false, ctaText }: PricingCardProps) => {
  return (
    <div className={`relative bg-white rounded-2xl p-8 shadow-lg hover:shadow-xl transition-all duration-300 border-2 ${
      isPopular ? 'border-blue-400 transform scale-105' : 'border-slate-100 hover:border-blue-200'
    }`}>
      {isPopular && (
        <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
          <span className="bg-gradient-to-r from-blue-500 to-green-500 text-white px-6 py-2 rounded-full text-sm font-medium">
            Most Popular
          </span>
        </div>
      )}
      
      <div className="text-center mb-6">
        <h3 className="text-2xl font-bold text-slate-800 mb-2">{title}</h3>
        <div className="flex items-baseline justify-center">
          <span className="text-4xl font-bold text-slate-900">R{price}</span>
          <span className="text-slate-600 ml-2">/{period}</span>
        </div>
      </div>

      <ul className="space-y-3 mb-8">
        {features.map((feature, index) => (
          <li key={index} className="flex items-center space-x-3">
            <div className="w-5 h-5 bg-green-100 rounded-full flex items-center justify-center">
              <Check className="h-3 w-3 text-green-600" />
            </div>
            <span className="text-slate-700">{feature}</span>
          </li>
        ))}
      </ul>

      <button className={`w-full py-4 px-6 rounded-xl font-semibold transition-all duration-200 ${
        isPopular 
          ? 'bg-gradient-to-r from-blue-500 to-green-500 text-white hover:from-blue-600 hover:to-green-600 shadow-lg hover:shadow-xl' 
          : 'bg-slate-100 text-slate-800 hover:bg-slate-200'
      }`}>
        {ctaText}
      </button>
    </div>
  );
};

export default PricingCard;
