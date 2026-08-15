import { useEffect, useState, type FormEvent } from 'react';
import Navigation from '../components/Navigation';
import { useAuth } from '../contexts/AuthContext';
import { apiClient } from '../services/api';
import type { BusinessDto, UpdateBusinessRequest } from '../types/api';

const fieldClass = 'w-full p-3 border border-slate-300 rounded-xl focus:ring-2 focus:ring-blue-500';
const emptyBusiness = { businessName: '', description: '', address: '', city: '', phone: '', email: '', website: '' };
const toForm = (business: BusinessDto) => ({
  businessName: business.businessName, description: business.description, address: business.address,
  city: business.city, phone: business.phone, email: business.email, website: business.website ?? '',
});

const Settings = () => {
  const { user, refreshProfile } = useAuth();
  const [businesses, setBusinesses] = useState<BusinessDto[]>([]);
  const [businessId, setBusinessId] = useState('');
  const [loading, setLoading] = useState(user?.type === 'provider');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [profile, setProfile] = useState({ fullName: user?.name ?? '', contactNumber: user?.contactNumber ?? '' });
  const [form, setForm] = useState(emptyBusiness);
  const business = businesses.find((item) => item.businessId === businessId) ?? null;

  useEffect(() => {
    if (user?.type !== 'provider') return;
    void apiClient.getMyBusinesses().then((response) => {
      setLoading(false);
      if (response.error) { setError(response.error); return; }
      const owned = response.data ?? [];
      setBusinesses(owned);
      if (owned[0]) setBusinessId((current) => current || owned[0].businessId);
    });
  }, [user?.type]);

  useEffect(() => {
    setForm(business ? toForm(business) : emptyBusiness);
  }, [business]);

  const update = (name: string, value: string) => setForm((current) => ({ ...current, [name]: value }));

  const saveProfile = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true); setError(''); setMessage('');
    const response = await apiClient.updateProfile({ fullName: profile.fullName.trim(), contactNumber: profile.contactNumber.trim() || null });
    setSaving(false);
    if (response.error) { setError(response.error); return; }
    try { await refreshProfile(); } catch (refreshError) {
      setError(refreshError instanceof Error ? refreshError.message : 'Profile was saved, but could not be refreshed.');
      return;
    }
    setMessage('Settings saved.');
  };

  const saveBusiness = async (event: FormEvent) => {
    event.preventDefault();
    setError(''); setMessage('');
    if (!form.businessName.trim() || !form.description.trim() || !form.address.trim() || !form.city.trim() || !form.phone.trim() || !form.email.trim()) {
      setError('Complete all required business fields before saving.');
      return;
    }
    setSaving(true);
    const payload: UpdateBusinessRequest = {
      businessName: form.businessName.trim(), description: form.description.trim(), address: form.address.trim(),
      city: form.city.trim(), phone: form.phone.trim(), email: form.email.trim(), website: form.website.trim() || null,
    };
    const response = business
      ? await apiClient.updateBusiness(business.businessId, payload)
      : await apiClient.createBusiness({
        businessName: payload.businessName!, description: payload.description!, address: payload.address!, city: payload.city!,
        phone: payload.phone!, email: payload.email!, website: payload.website, isActive: true,
      });
    setSaving(false);
    if (response.error || !response.data) { setError(response.error || 'Business settings could not be saved.'); return; }
    const saved = response.data;
    setBusinesses((current) => current.some((item) => item.businessId === saved.businessId)
      ? current.map((item) => item.businessId === saved.businessId ? saved : item)
      : [...current, saved]);
    setBusinessId(saved.businessId);
    setMessage('Settings saved.');
  };

  const field = (name: keyof typeof form, label: string, type = 'text', required = true) => (
    <div><label htmlFor={name} className="block text-sm font-medium mb-2">{label}{required ? ' *' : ''}</label><input id={name} type={type} required={required} value={form[name]} onChange={(e) => update(name, e.target.value)} className={fieldClass} /></div>
  );

  return (
    <div className="min-h-screen bg-slate-50"><Navigation />
      <main className="max-w-4xl mx-auto px-4 sm:px-6 py-8">
        <h1 className="text-3xl font-bold">Settings</h1><p className="text-slate-600 mt-1">Changes are saved to your BookSpot account.</p>
        {error && <div role="alert" className="mt-5 bg-red-50 border border-red-200 text-red-800 p-4 rounded-xl">{error}</div>}
        {message && <div role="status" className="mt-5 bg-green-50 border border-green-200 text-green-800 p-4 rounded-xl">{message}</div>}
        {loading ? <p role="status" className="mt-8">Loading settings…</p> : user?.type === 'provider' ? (
          <form onSubmit={saveBusiness} className="mt-8 bg-white border rounded-2xl p-5 sm:p-8 grid grid-cols-1 sm:grid-cols-2 gap-5">
            <h2 className="text-xl font-bold sm:col-span-2">{business ? 'Business profile' : 'Create your business profile'}</h2>
            {businesses.length > 0 && <div className="sm:col-span-2"><label htmlFor="business-selector" className="block text-sm font-medium mb-2">Business to edit</label><select id="business-selector" value={businessId} onChange={(event) => setBusinessId(event.target.value)} className={fieldClass}>{businesses.map((item) => <option key={item.businessId} value={item.businessId}>{item.businessName}</option>)}<option value="">Create another business</option></select></div>}
            {field('businessName', 'Business name')}{field('phone', 'Business phone', 'tel')}{field('email', 'Business email', 'email')}{field('city', 'City')}
            <div className="sm:col-span-2">{field('address', 'Address')}</div>
            <div className="sm:col-span-2"><label htmlFor="description" className="block text-sm font-medium mb-2">Description *</label><textarea id="description" required rows={4} value={form.description} onChange={(e) => update('description', e.target.value)} className={fieldClass} /></div>
            <div className="sm:col-span-2">{field('website', 'Website', 'url', false)}</div>
            <div className="sm:col-span-2 flex justify-end"><button type="submit" disabled={saving} className="bg-blue-700 text-white px-6 py-3 rounded-xl disabled:opacity-60">{saving ? 'Saving…' : 'Save business settings'}</button></div>
          </form>
        ) : (
          <form onSubmit={saveProfile} className="mt-8 bg-white border rounded-2xl p-5 sm:p-8 space-y-5">
            <h2 className="text-xl font-bold">Profile</h2>
            <div><label htmlFor="fullName" className="block text-sm font-medium mb-2">Full name</label><input id="fullName" value={profile.fullName} onChange={(e) => setProfile((current) => ({ ...current, fullName: e.target.value }))} className={fieldClass} /></div>
            <div><label htmlFor="contactNumber" className="block text-sm font-medium mb-2">Contact number</label><input id="contactNumber" type="tel" value={profile.contactNumber} onChange={(e) => setProfile((current) => ({ ...current, contactNumber: e.target.value }))} className={fieldClass} /></div>
            <div><label htmlFor="email" className="block text-sm font-medium mb-2">Email</label><input id="email" value={user?.email ?? ''} readOnly className={`${fieldClass} bg-slate-100`} /></div>
            <button type="submit" disabled={saving} className="bg-blue-700 text-white px-6 py-3 rounded-xl disabled:opacity-60">{saving ? 'Saving…' : 'Save profile settings'}</button>
          </form>
        )}
      </main>
    </div>
  );
};

export default Settings;
