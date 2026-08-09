import { useEffect, useMemo, useState } from 'react';
import { TrendingUp, Calendar, Clock, Star, Loader2 } from 'lucide-react';
import { DataSourceAdapter } from '../services/dataSourceAdapter';
import { useAuth } from '../contexts/AuthContext';
import type { DashboardStats, BookingWithDetails } from '../types/api';

interface PopularService {
  id: string;
  name: string;
  bookings: number;
  revenue: number;
}

interface RecentMetric {
  label: string;
  value: string;
  highlight?: boolean;
}

const formatCurrency = (value: number) => `R${value.toLocaleString('en-ZA', { minimumFractionDigits: 0 })}`;

const BusinessInsights = () => {
  const { user, isLoggedIn } = useAuth();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [popularServices, setPopularServices] = useState<PopularService[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchInsights = async () => {
      if (!isLoggedIn || !user) {
        setError('Please log in to view business insights.');
        setStats(null);
        setPopularServices([]);
        setLoading(false);
        return;
      }

      if (user.type !== 'provider') {
        setError('Business insights are only available to provider accounts.');
        setStats(null);
        setPopularServices([]);
        setLoading(false);
        return;
      }

      setLoading(true);
      setError(null);
      const isClientType = user.type === 'provider' ? false : true;
      try {
        const [statsResponse, bookingsResponse] = await Promise.all([
          DataSourceAdapter.getDashboardStats(user.id),
          DataSourceAdapter.getProviderBookings(user.id, undefined, undefined, undefined, isClientType),
        ]);

        if (statsResponse.error) {
          throw new Error(statsResponse.error);
        }
        setStats(statsResponse.data ?? null);

        if (bookingsResponse.error) {
          throw new Error(bookingsResponse.error);
        }

        const bookings: BookingWithDetails[] = bookingsResponse.data ?? [];
        const serviceTotals = new Map<string, PopularService>();

        bookings.forEach((booking) => {
          if (!booking.service) return;
          const existing = serviceTotals.get(booking.service.id) ?? {
            id: booking.service.id,
            name: booking.service.name,
            bookings: 0,
            revenue: 0,
          };
          existing.bookings += 1;
          existing.revenue += booking.service.price ?? 0;
          serviceTotals.set(booking.service.id, existing);
        });

        const topServices = Array.from(serviceTotals.values())
          .sort((a, b) => {
            if (b.bookings === a.bookings) {
              return b.revenue - a.revenue;
            }
            return b.bookings - a.bookings;
          })
          .slice(0, 5);

        setPopularServices(topServices);
      } catch (err) {
        console.error('Failed to load business insights:', err);
        setStats(null);
        setPopularServices([]);
        setError(err instanceof Error ? err.message : 'Failed to load business insights.');
      } finally {
        setLoading(false);
      }
    };

    fetchInsights();
  }, [isLoggedIn, user]);

  const insights = useMemo(() => [
    {
      title: 'Monthly Revenue',
      value: stats ? formatCurrency(stats.monthlyRevenue) : 'N/A',
      description: 'Revenue from completed bookings this month',
      icon: TrendingUp,
      color: 'text-green-600',
      bgColor: 'bg-green-100',
    },
    {
      title: "Today's Bookings",
      value: stats ? stats.todayBookings.toString() : 'N/A',
      description: 'Scheduled for today',
      icon: Calendar,
      color: 'text-blue-600',
      bgColor: 'bg-blue-100',
    },
    {
      title: 'Weekly Bookings',
      value: stats ? stats.weekBookings.toString() : 'N/A',
      description: 'Bookings over the last 7 days',
      icon: Clock,
      color: 'text-purple-600',
      bgColor: 'bg-purple-100',
    },
    {
      title: 'Total Clients',
      value: stats ? stats.totalClients.toString() : 'N/A',
      description: 'Clients served to date',
      icon: Star,
      color: 'text-yellow-600',
      bgColor: 'bg-yellow-100',
    },
  ], [stats]);

  const recentMetrics: RecentMetric[] = useMemo(() => [
    {
      label: 'Pending bookings',
      value: stats ? stats.pendingBookings.toString() : 'N/A',
    },
    {
      label: 'Confirmed bookings',
      value: stats ? stats.confirmedBookings.toString() : 'N/A',
      highlight: true,
    },
    {
      label: 'Monthly revenue',
      value: stats ? formatCurrency(stats.monthlyRevenue) : 'N/A',
    },
    {
      label: 'Total clients',
      value: stats ? stats.totalClients.toString() : 'N/A',
    },
  ], [stats]);

  return (
    <div className="space-y-6">
      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {/* Key Insights */}
      <div className="bg-white rounded-2xl shadow-lg border border-slate-100 p-6">
        <h2 className="text-xl font-bold text-slate-900 mb-6">Business Insights</h2>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {insights.map((insight, index) => (
            <div key={index} className="text-center">
              <div className={`w-12 h-12 ${insight.bgColor} rounded-xl flex items-center justify-center mx-auto mb-3`}>
                <insight.icon className={`h-6 w-6 ${insight.color}`} />
              </div>
              <div className="text-2xl font-bold text-slate-900">{insight.value}</div>
              <div className="text-sm text-slate-600">{insight.title}</div>
              <div className="text-xs text-slate-500 mt-1">{insight.description}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Recent Metrics */}
      <div className="bg-white rounded-2xl shadow-lg border border-slate-100 p-6">
        <h3 className="text-lg font-semibold text-slate-900 mb-4">At a Glance</h3>
        <div className="grid grid-cols-2 gap-4">
          {recentMetrics.map((metric, index) => (
            <div key={index} className="p-4 bg-slate-50 rounded-xl">
              <div className="text-sm text-slate-600">{metric.label}</div>
              <div className={`text-xl font-bold ${metric.highlight ? 'text-blue-600' : 'text-slate-900'}`}>
                {metric.value}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Popular Services */}
      <div className="bg-white rounded-2xl shadow-lg border border-slate-100 p-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-slate-900">Popular Services</h3>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-blue-500" />
            <span className="ml-2 text-slate-600">Loading services...</span>
          </div>
        ) : popularServices.length > 0 ? (
          <div className="space-y-3">
            {popularServices.map((service) => (
              <div key={service.id} className="flex items-center justify-between p-3 bg-slate-50 rounded-xl">
                <div>
                  <div className="font-medium text-slate-900">{service.name}</div>
                  <div className="text-sm text-slate-600">{service.bookings} bookings</div>
                </div>
                <div className="text-right">
                  <div className="font-bold text-slate-900">{formatCurrency(service.revenue)}</div>
                  <div className="text-sm text-slate-600">revenue</div>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-center py-8">
            <p className="text-slate-600">No popular services data available yet.</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default BusinessInsights;

