// Dashboard-specific hooks using TanStack Query
import { useQuery } from '@tanstack/react-query';
import { DataSourceAdapter } from '../services/dataSourceAdapter';
import { DashboardStats, UserType } from '../types/api';

// Query keys for consistent caching
export const dashboardKeys = {
    all: ['dashboard'] as const,
    stats: (providerId: string) => [...dashboardKeys.all, 'stats', providerId] as const,
};

// Hook for getting dashboard statistics
export const useDashboardStats = (providerId: string, isClient?: boolean) => {
    return useQuery({
        queryKey: dashboardKeys.stats(providerId),
        queryFn: async () => {
            const response = await DataSourceAdapter.getDashboardStats(providerId, isClient);
            if (response.error) throw new Error(response.error);
            return response.data;
        },
        enabled: !!providerId,
        staleTime: 2 * 60 * 1000, // 2 minutes - stats should be relatively fresh
        refetchInterval: 5 * 60 * 1000, // Refetch every 5 minutes
    });
};

// Helper hook to get formatted stats for display
export const useFormattedDashboardStats = (providerId: string, isClient?: boolean) => {
    const { data: stats, ...queryResult } = useDashboardStats(providerId, isClient);

    const formattedStats = stats ? [
        {
            title: 'Today\'s Bookings',
            value: stats?.todayBookings?.toString(),
            icon: 'Calendar',
            color: 'bg-blue-500'
        },
        {
            title: 'This Week',
            value: stats?.weekBookings?.toString(),
            icon: 'Clock',
            color: 'bg-green-500'
        },
        {
            title: 'Total Clients',
            value: stats?.totalClients?.toString(),
            icon: 'Users',
            color: 'bg-purple-500'
        },
        {
            title: 'Revenue (Month)',
            value: `R  ${stats?.monthlyRevenue?.toLocaleString() ?? 0}`,
            icon: 'TrendingUp',
            color: 'bg-orange-500'
        }
    ] : [];

    return {
        ...queryResult,
        data: stats,
        formattedStats,
    };
};