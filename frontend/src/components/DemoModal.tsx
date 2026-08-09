
import React from 'react';
import { X, Play, Calendar, Users, Clock, BarChart3 } from 'lucide-react';

interface DemoModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const DemoModal = ({ isOpen, onClose }: DemoModalProps) => {
  if (!isOpen) return null;

  const demoFeatures = [
    {
      icon: Calendar,
      title: "Smart Scheduling",
      description: "See how clients can book appointments 24/7 with your custom booking page"
    },
    {
      icon: Users,
      title: "Client Management",
      description: "Track client history, preferences, and manage your customer relationships"
    },
    {
      icon: Clock,
      title: "Time Management",
      description: "Set availability, manage time slots, and handle multiple services effortlessly"
    },
    {
      icon: BarChart3,
      title: "Business Analytics",
      description: "Monitor your bookings, revenue trends, and grow your business with insights"
    }
  ];

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-2xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-slate-200">
          <div className="flex items-center space-x-3">
            <div className="w-12 h-12 bg-gradient-to-br from-blue-400 to-green-400 rounded-full flex items-center justify-center">
              <Play className="h-6 w-6 text-white" />
            </div>
            <div>
              <h2 className="text-2xl font-bold text-slate-900">HirePros Demo</h2>
              <p className="text-slate-600">See how HirePros transforms your business</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 hover:bg-slate-100 rounded-full transition-colors"
          >
            <X className="h-6 w-6 text-slate-600" />
          </button>
        </div>

        {/* Demo Video Placeholder */}
        <div className="p-6">
          <div className="bg-gradient-to-br from-blue-100 to-green-100 rounded-xl p-12 text-center mb-8">
            <div className="w-24 h-24 bg-white rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg">
              <Play className="h-12 w-12 text-blue-600" />
            </div>
            <h3 className="text-2xl font-bold text-slate-900 mb-4">
              Interactive Demo Coming Soon!
            </h3>
            <p className="text-lg text-slate-600 mb-6">
              Experience how HirePros revolutionizes appointment booking for service providers
            </p>
            <div className="bg-white rounded-lg p-4 text-sm text-slate-500">
              🎥 Watch a 3-minute walkthrough of all features<br/>
              📱 See the mobile-first design in action<br/>
              📊 Explore the business analytics dashboard
            </div>
          </div>

          {/* Feature Highlights */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
            {demoFeatures.map((feature, index) => (
              <div key={index} className="flex items-start space-x-4 p-4 bg-slate-50 rounded-xl">
                <div className="w-12 h-12 bg-gradient-to-br from-blue-400 to-green-400 rounded-full flex items-center justify-center flex-shrink-0">
                  <feature.icon className="h-6 w-6 text-white" />
                </div>
                <div>
                  <h4 className="font-semibold text-slate-900 mb-2">{feature.title}</h4>
                  <p className="text-slate-600 text-sm">{feature.description}</p>
                </div>
              </div>
            ))}
          </div>

          {/* CTA */}
          <div className="text-center">
            <button
              onClick={onClose}
              className="bg-gradient-to-r from-blue-500 to-green-500 text-white px-8 py-3 rounded-full font-semibold hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl mr-4"
            >
              Start Your Free Trial
            </button>
            <button
              onClick={onClose}
              className="text-slate-600 px-6 py-3 rounded-full font-medium hover:bg-slate-100 transition-colors"
            >
              Close Demo
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DemoModal;
