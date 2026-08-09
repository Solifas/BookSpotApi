import React, { useState } from 'react';
import { X, Mail, ArrowLeft } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from './ui/dialog';
import { apiClient } from '../services/api';
import { toast } from 'sonner';

interface ForgotPasswordModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const ForgotPasswordModal = ({ isOpen, onClose }: ForgotPasswordModalProps) => {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [emailSent, setEmailSent] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await apiClient.requestPasswordReset(email);
      
      if (response.error) {
        toast.error(response.error);
      } else {
        setEmailSent(true);
        toast.success('Password reset email sent! Please check your inbox.');
      }
    } catch (error) {
      toast.error('Failed to send reset email. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setEmail('');
    setEmailSent(false);
    onClose();
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-2xl font-bold text-slate-900">
            {emailSent ? 'Check Your Email' : 'Reset Password'}
          </DialogTitle>
        </DialogHeader>

        {!emailSent ? (
          <form onSubmit={handleSubmit} className="space-y-4 mt-4">
            <p className="text-slate-600 text-sm">
              Enter your email address and we'll send you a link to reset your password.
            </p>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Email Address
              </label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-10 pr-4 py-3 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  placeholder="your.email@example.com"
                  required
                />
              </div>
            </div>

            <div className="flex gap-3">
              <button
                type="button"
                onClick={handleClose}
                className="flex-1 px-4 py-3 border border-slate-300 text-slate-700 rounded-xl font-medium hover:bg-slate-50 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={loading}
                className="flex-1 bg-gradient-to-r from-blue-500 to-green-500 text-white py-3 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200 disabled:opacity-50"
              >
                {loading ? 'Sending...' : 'Send Reset Link'}
              </button>
            </div>
          </form>
        ) : (
          <div className="space-y-4 mt-4">
            <div className="bg-green-50 border border-green-200 rounded-xl p-4">
              <p className="text-green-800 text-sm">
                We've sent a password reset link to <strong>{email}</strong>. 
                Please check your inbox and follow the instructions.
              </p>
            </div>

            <p className="text-slate-600 text-sm">
              Didn't receive the email? Check your spam folder or try again.
            </p>

            <button
              onClick={handleClose}
              className="w-full bg-gradient-to-r from-blue-500 to-green-500 text-white py-3 rounded-xl font-medium hover:from-blue-600 hover:to-green-600 transition-all duration-200"
            >
              Back to Login
            </button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
};

export default ForgotPasswordModal;
