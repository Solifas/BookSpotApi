import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { apiClient, createIdempotencyKey } from '@/services/api';
import { getResetTokenFromLocation, validateResetPassword, type FieldErrors } from '@/lib/authValidation';

const ResetPassword = () => {
  const navigate = useNavigate();
  const [token] = useState(() => getResetTokenFromLocation(window.location.hash, window.location.search));
  const [state, setState] = useState<'validating' | 'ready' | 'invalid' | 'unavailable' | 'complete'>('validating');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [errors, setErrors] = useState<FieldErrors>({});
  const [serverError, setServerError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const resetOperationKey = useRef<string | null>(null);

  const validateToken = useCallback(async () => {
    if (!token) {
      setState('invalid');
      return;
    }
    setState('validating');
    const response = await apiClient.validateResetToken(token);
    if (response.data?.valid) setState('ready');
    else if (response.status === 400) setState('invalid');
    else setState('unavailable');
  }, [token]);

  useEffect(() => {
    window.history.replaceState(null, '', window.location.pathname);
    void validateToken();
  }, [validateToken]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!token) return;
    const validation = validateResetPassword(password, confirmPassword);
    setErrors(validation);
    setServerError('');
    if (Object.keys(validation).length) return;

    setSubmitting(true);
    resetOperationKey.current ??= createIdempotencyKey();
    const response = await apiClient.resetPassword(token, password, resetOperationKey.current);
    setSubmitting(false);
    if (response.error) {
      if (response.status !== 0 && response.status !== 503) resetOperationKey.current = null;
      setServerError(response.error);
      return;
    }
    resetOperationKey.current = null;
    setState('complete');
  };

  return (
    <main className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-green-50 flex items-center justify-center p-4">
      <section className="w-full max-w-md bg-white rounded-2xl shadow-xl p-6 sm:p-8" aria-live="polite">
        <h1 className="text-3xl font-bold text-slate-900">Reset password</h1>
        {state === 'validating' && <p className="mt-4 text-slate-600">Checking your reset link…</p>}
        {state === 'invalid' && (
          <div className="mt-5" role="alert">
            <p className="text-red-800">This reset link is invalid or no longer available.</p>
            <Link to="/login" className="inline-block mt-4 text-blue-700 font-medium">Request a new reset link</Link>
          </div>
        )}
        {state === 'unavailable' && (
          <div className="mt-5" role="alert">
            <p className="text-amber-900">We could not check this reset link right now. The link may still be valid.</p>
            <button type="button" onClick={() => void validateToken()} className="mt-4 text-blue-700 font-medium">Try validation again</button>
          </div>
        )}
        {state === 'complete' && (
          <div className="mt-5">
            <p className="text-green-800">Password reset completed.</p>
            <button onClick={() => navigate('/login')} className="mt-5 w-full bg-blue-600 text-white py-3 rounded-xl">Sign in with your new password</button>
          </div>
        )}
        {state === 'ready' && (
          <form onSubmit={submit} noValidate className="space-y-5 mt-6">
            <div>
              <label htmlFor="new-password" className="block text-sm font-medium mb-2">New password</label>
              <input id="new-password" type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} aria-invalid={Boolean(errors.password)} aria-describedby={errors.password ? 'password-error' : undefined} className="w-full p-3 border rounded-xl" />
              {errors.password && <p id="password-error" role="alert" className="text-red-700 text-sm mt-1">{errors.password}</p>}
            </div>
            <div>
              <label htmlFor="confirm-password" className="block text-sm font-medium mb-2">Confirm new password</label>
              <input id="confirm-password" type="password" autoComplete="new-password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} aria-invalid={Boolean(errors.confirmPassword)} aria-describedby={errors.confirmPassword ? 'confirm-error' : undefined} className="w-full p-3 border rounded-xl" />
              {errors.confirmPassword && <p id="confirm-error" role="alert" className="text-red-700 text-sm mt-1">{errors.confirmPassword}</p>}
            </div>
            {serverError && <div role="alert" className="bg-red-50 border border-red-200 text-red-800 p-3 rounded-lg">{serverError}</div>}
            <button type="submit" disabled={submitting} className="w-full bg-gradient-to-r from-blue-500 to-green-500 text-white py-3 rounded-xl disabled:opacity-60">{submitting ? 'Resetting…' : 'Reset password'}</button>
          </form>
        )}
      </section>
    </main>
  );
};

export default ResetPassword;
