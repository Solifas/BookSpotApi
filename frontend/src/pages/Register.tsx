import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Calendar } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { validateRegistration, type FieldErrors } from '../lib/authValidation';
import type { UserTypeValue } from '../types/api';

const Register = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { register } = useAuth();
  const initialType: UserTypeValue = searchParams.get('type') === 'provider' ? 'provider' : 'client';
  const [fields, setFields] = useState({
    fullName: '', email: '', contactNumber: '', password: '', confirmPassword: '', userType: initialType,
  });
  const [errors, setErrors] = useState<FieldErrors>({});
  const [serverError, setServerError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const update = (name: string, value: string) => {
    setFields((current) => ({ ...current, [name]: value }));
    setErrors((current) => ({ ...current, [name]: '' }));
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const validation = validateRegistration(fields);
    setErrors(validation);
    setServerError('');
    if (Object.keys(validation).length) return;

    setSubmitting(true);
    try {
      await register({
        email: fields.email.trim(),
        fullName: fields.fullName.trim(),
        contactNumber: fields.contactNumber.trim() || null,
        password: fields.password,
        userType: fields.userType,
      });
      navigate('/dashboard');
    } catch (error) {
      setServerError(error instanceof Error ? error.message : 'Registration failed. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const input = (name: 'fullName' | 'email' | 'contactNumber' | 'password' | 'confirmPassword', label: string, type = 'text') => (
    <div>
      <label htmlFor={name} className="block text-sm font-medium text-slate-700 mb-2">{label}</label>
      <input
        id={name}
        name={name}
        type={type}
        value={fields[name]}
        onChange={(event) => update(name, event.target.value)}
        aria-invalid={Boolean(errors[name])}
        aria-describedby={errors[name] ? `${name}-error` : undefined}
        autoComplete={name === 'confirmPassword' ? 'new-password' : name === 'password' ? 'new-password' : name}
        className="w-full px-4 py-3 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500"
      />
      {errors[name] && <p id={`${name}-error`} role="alert" className="text-sm text-red-700 mt-1">{errors[name]}</p>}
    </div>
  );

  return (
    <main className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-green-50 flex items-center justify-center p-4">
      <div className="max-w-md w-full">
        <header className="text-center mb-6">
          <Link to="/" className="text-slate-600 hover:text-blue-700">← Back to Home</Link>
          <div className="flex items-center justify-center gap-2 mt-5"><Calendar aria-hidden className="text-blue-600" /><span className="text-2xl font-bold">HirePros</span></div>
          <h1 className="text-3xl font-bold text-slate-900 mt-4">Create account</h1>
        </header>
        <section className="bg-white rounded-2xl shadow-xl p-6 sm:p-8">
          <form onSubmit={submit} noValidate className="space-y-5">
            <fieldset>
              <legend className="text-sm font-medium text-slate-700 mb-2">Account type</legend>
              <div className="grid grid-cols-2 gap-3">
                {(['client', 'provider'] as const).map((type) => (
                  <button key={type} type="button" aria-pressed={fields.userType === type} onClick={() => setFields((current) => ({ ...current, userType: type }))}
                    className={`p-3 rounded-xl border-2 capitalize ${fields.userType === type ? 'border-blue-500 bg-blue-50' : 'border-slate-200'}`}>
                    {type}
                  </button>
                ))}
              </div>
            </fieldset>
            {input('fullName', 'Full name')}
            {input('email', 'Email address', 'email')}
            {input('contactNumber', 'Contact number (optional)', 'tel')}
            {input('password', 'Password', 'password')}
            <p className="text-xs text-slate-600 -mt-3">Use 15–64 characters.</p>
            {input('confirmPassword', 'Confirm password', 'password')}
            {serverError && <div role="alert" className="bg-red-50 border border-red-200 text-red-800 p-3 rounded-lg">{serverError}</div>}
            <button type="submit" disabled={submitting} className="w-full bg-gradient-to-r from-blue-500 to-green-500 text-white py-3 rounded-xl font-medium disabled:opacity-60">
              {submitting ? 'Creating account…' : `Create ${fields.userType} account`}
            </button>
          </form>
          <p className="mt-6 text-center text-slate-600">Already registered? <Link to="/login" className="text-blue-700 font-medium">Sign in</Link></p>
        </section>
      </div>
    </main>
  );
};

export default Register;
