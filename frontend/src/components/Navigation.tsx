import React, { useState } from 'react';
import { Calendar, Menu, X, Settings, Home, LogIn, LogOut, User } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import logo from '../assets/hirepros-logo.png';

const Navigation = () => {
  const [isOpen, setIsOpen] = useState(false);
  const location = useLocation();
  const { user, logout, isLoggedIn } = useAuth();

  const isActive = (path: string) => location.pathname === path;

  const handleLogout = () => {
    logout();
    setIsOpen(false);
  };

  return (
    <>
      <nav className="bg-white border-b border-slate-200 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-3 sm:px-4 lg:px-8">
          <div className="flex justify-between h-14 sm:h-16">
            <div className="flex items-center">
              <Link to="/" className="flex items-center space-x-2">
                <img src={logo} alt="HirePros Logo" className="w-8 h-8 sm:w-10 sm:h-10" />
                <span className="text-lg sm:text-xl font-bold text-slate-800">HirePros</span>
              </Link>
            </div>

            {/* Desktop Navigation */}
            <div className="hidden md:flex items-center space-x-6 lg:space-x-8">
              <Link 
                to="/" 
                className={`flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors text-sm ${
                  isActive('/') ? 'bg-blue-50 text-blue-600' : 'text-slate-700 hover:text-blue-600 hover:bg-blue-50'
                }`}
              >
                <Home className="h-4 w-4" />
                <span>Home</span>
              </Link>
              
              {isLoggedIn && (
                <Link 
                  to="/dashboard" 
                  className={`flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors text-sm ${
                    isActive('/dashboard') ? 'bg-blue-50 text-blue-600' : 'text-slate-700 hover:text-blue-600 hover:bg-blue-50'
                  }`}
                >
                  <Calendar className="h-4 w-4" />
                  <span>{user?.type === 'client' ? 'My Bookings' : 'Dashboard'}</span>
                </Link>
              )}
              
              {isLoggedIn && (
                <Link 
                  to="/settings" 
                  className={`flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors text-sm ${
                    isActive('/settings') ? 'bg-blue-50 text-blue-600' : 'text-slate-700 hover:text-blue-600 hover:bg-blue-50'
                  }`}
                >
                  <Settings className="h-4 w-4" />
                  <span>{user?.type === 'client' ? 'Settings' : 'Settings'}</span>
                </Link>
              )}

              {isLoggedIn ? (
                <div className="flex items-center space-x-3 lg:space-x-4">
                  <div className="flex items-center space-x-2 text-slate-700">
                    <User className="h-4 w-4" />
                    <span className="font-medium text-sm">{user?.name}</span>
                    <span className="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded-full">
                      {user?.type}
                    </span>
                  </div>
                  <button 
                    onClick={handleLogout}
                    className="flex items-center space-x-2 text-slate-700 hover:text-red-600 px-3 py-2 rounded-lg hover:bg-red-50 transition-colors text-sm"
                  >
                    <LogOut className="h-4 w-4" />
                    <span>Logout</span>
                  </button>
                </div>
              ) : (
                <div className="flex items-center space-x-3 lg:space-x-4">
                  <Link 
                    to="/login"
                    className="flex items-center space-x-2 text-slate-700 hover:text-blue-600 px-3 py-2 rounded-lg hover:bg-blue-50 transition-colors text-sm"
                  >
                    <LogIn className="h-4 w-4" />
                    <span>Login</span>
                  </Link>
                  <Link 
                    to="/register"
                    className="bg-gradient-to-r from-blue-500 to-green-500 text-white px-4 lg:px-6 py-2 rounded-full font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg hover:shadow-xl text-sm"
                  >
                    Get Started
                  </Link>
                </div>
              )}
            </div>

            {/* Mobile menu button */}
            <div className="md:hidden flex items-center">
              <button
                onClick={() => setIsOpen(!isOpen)}
                className="p-2 rounded-lg text-slate-700 hover:text-blue-600 hover:bg-blue-50 transition-colors"
              >
                {isOpen ? <X className="h-5 w-5 sm:h-6 sm:w-6" /> : <Menu className="h-5 w-5 sm:h-6 sm:w-6" />}
              </button>
            </div>
          </div>
        </div>

        {/* Mobile Navigation */}
        {isOpen && (
          <div className="md:hidden bg-white border-t border-slate-200 shadow-lg">
            <div className="px-3 py-3 space-y-2 max-h-screen overflow-y-auto">
              <Link 
                to="/" 
                onClick={() => setIsOpen(false)}
                className={`flex items-center space-x-3 px-3 py-3 rounded-lg transition-colors ${
                  isActive('/') ? 'bg-blue-50 text-blue-600' : 'text-slate-700 hover:text-blue-600 hover:bg-blue-50'
                }`}
              >
                <Home className="h-5 w-5" />
                <span className="font-medium">Home</span>
              </Link>
              
              {isLoggedIn && (
                <Link 
                  to="/dashboard" 
                  onClick={() => setIsOpen(false)}
                  className={`flex items-center space-x-3 px-3 py-3 rounded-lg transition-colors ${
                    isActive('/dashboard') ? 'bg-blue-50 text-blue-600' : 'text-slate-700 hover:text-blue-600 hover:bg-blue-50'
                  }`}
                >
                  <Calendar className="h-5 w-5" />
                  <span className="font-medium">{user?.type === 'client' ? 'My Bookings' : 'Dashboard'}</span>
                </Link>
              )}
              
              {isLoggedIn && (
                <Link 
                  to="/settings" 
                  onClick={() => setIsOpen(false)}
                  className={`flex items-center space-x-3 px-3 py-3 rounded-lg transition-colors ${
                    isActive('/settings') ? 'bg-blue-50 text-blue-600' : 'text-slate-700 hover:text-blue-600 hover:bg-blue-50'
                  }`}
                >
                  <Settings className="h-5 w-5" />
                  <span className="font-medium">{user?.type === 'client' ? 'Settings' : 'Settings'}</span>
                </Link>
              )}

              {isLoggedIn ? (
                <div className="pt-3 border-t border-slate-200 space-y-2">
                  <div className="flex items-center space-x-3 px-3 py-2 text-slate-700 bg-slate-50 rounded-lg">
                    <User className="h-5 w-5" />
                    <div className="flex-1">
                      <span className="font-medium block">{user?.name}</span>
                      <span className="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded-full inline-block mt-1">
                        {user?.type}
                      </span>
                    </div>
                  </div>
                  <button 
                    onClick={handleLogout}
                    className="w-full flex items-center space-x-3 px-3 py-3 rounded-lg text-red-600 hover:bg-red-50 transition-colors"
                  >
                    <LogOut className="h-5 w-5" />
                    <span className="font-medium">Logout</span>
                  </button>
                </div>
              ) : (
                <div className="pt-3 border-t border-slate-200 space-y-2">
                  <Link 
                    to="/login"
                    onClick={() => setIsOpen(false)}
                    className="w-full flex items-center space-x-3 px-3 py-3 rounded-lg text-slate-700 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                  >
                    <LogIn className="h-5 w-5" />
                    <span className="font-medium">Login</span>
                  </Link>
                  <Link 
                    to="/register"
                    onClick={() => setIsOpen(false)}
                    className="w-full bg-gradient-to-r from-blue-500 to-green-500 text-white px-4 py-3 rounded-lg font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 shadow-lg text-center block"
                  >
                    Get Started
                  </Link>
                </div>
              )}
            </div>
          </div>
        )}
      </nav>
    </>
  );
};

export default Navigation;
