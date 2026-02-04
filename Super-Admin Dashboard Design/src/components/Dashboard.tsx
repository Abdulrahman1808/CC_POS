import React, { useEffect, useState } from 'react';
import { Building2, DollarSign, Globe, UserPlus, TrendingUp } from 'lucide-react';
import { StatCard } from './StatCard';
import { LineChart } from './LineChart';
import { BarChart } from './BarChart';
import { Tag } from './Tag';
import { useI18n } from '../utils/I18nContext';

export function Dashboard() {
  const { t } = useI18n();
  const [liveActivity, setLiveActivity] = useState([
    { id: 1, tenant: 'Cafe XYZ', amount: 150, method: 'Fawry', time: '30 seconds ago' },
    { id: 2, tenant: 'Nile Books', amount: 320, method: 'Cash', time: '1 minute ago' },
    { id: 3, tenant: 'Bite & Brew', amount: 275, method: 'Card', time: '2 minutes ago' },
    { id: 4, tenant: 'Tech Hub Store', amount: 890, method: 'Fawry', time: '3 minutes ago' },
    { id: 5, tenant: 'Green Market', amount: 430, method: 'Cash', time: '5 minutes ago' }
  ]);

  // Simulate real-time updates
  useEffect(() => {
    const interval = setInterval(() => {
      const tenants = ['Cafe XYZ', 'Nile Books', 'Bite & Brew', 'Tech Hub Store', 'Green Market', 'Fashion Corner'];
      const methods = ['Fawry', 'Cash', 'Card'];

      const newActivity = {
        id: Date.now(),
        tenant: tenants[Math.floor(Math.random() * tenants.length)],
        amount: Math.floor(Math.random() * 500) + 50,
        method: methods[Math.floor(Math.random() * methods.length)],
        time: 'just now'
      };

      setLiveActivity(prev => [newActivity, ...prev.slice(0, 9)]);
    }, 5000); // New sale every 5 seconds

    return () => clearInterval(interval);
  }, []);

  // Sample data for charts
  const revenueData = [
    { date: 'Nov 1', revenue: 38000 },
    { date: 'Nov 5', revenue: 42000 },
    { date: 'Nov 10', revenue: 39000 },
    { date: 'Nov 15', revenue: 45000 },
    { date: 'Nov 20', revenue: 48000 },
    { date: 'Nov 25', revenue: 43000 },
    { date: 'Nov 30', revenue: 45000 }
  ];

  const growthData = [
    { month: 'Aug', tenants: 8 },
    { month: 'Sep', tenants: 15 },
    { month: 'Oct', tenants: 10 },
    { month: 'Nov', tenants: 12 }
  ];

  const topTenants = [
    { name: 'Bite & Brew Cafe', revenue: 150000 },
    { name: 'Nile Books', revenue: 120000 },
    { name: 'Tech Hub Store', revenue: 98000 },
    { name: 'Fashion Corner', revenue: 87000 },
    { name: 'Green Market', revenue: 76000 }
  ];

  return (
    <div className="space-y-6">
      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title={t('total_tenants')}
          value="1,420"
          icon={<Building2 size={24} />}
          trend={{ value: '12% from last month', isPositive: true }}
        />
        <StatCard
          title={t('revenue_today')}
          value="EGP 45,000"
          icon={<DollarSign size={24} />}
          trend={{ value: '8% from yesterday', isPositive: true }}
        />
        <StatCard
          title={t('platform_sales')}
          value="EGP 1.2M"
          icon={<Globe size={24} />}
          trend={{ value: 'All time', isPositive: true }}
        />
        <StatCard
          title={t('new_tenants')}
          value="12"
          icon={<UserPlus size={24} />}
          trend={{ value: '20% from last month', isPositive: true }}
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <LineChart
            data={revenueData}
            dataKey="revenue"
            xAxisKey="date"
            title={t('platform_sales') + " (Last 30 Days)"}
          />
        </div>
        <div>
          <BarChart
            data={growthData}
            dataKey="tenants"
            xAxisKey="month"
            title="Tenant Growth"
          />
        </div>
      </div>

      {/* Live Activity & Top Tenants */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Live Activity */}
        <div className="bg-card border border-border rounded-lg p-6">
          <div className="flex items-center gap-2 mb-4">
            <h3 className="text-card-foreground">{t('recent_activity')}</h3>
            <div className="flex items-center gap-1.5">
              <div className="w-2 h-2 bg-success rounded-full animate-pulse"></div>
              <span className="text-xs text-success">Real-time</span>
            </div>
          </div>

          <div className="space-y-3 max-h-80 overflow-y-auto">
            {liveActivity.map((activity) => (
              <div key={activity.id} className="border border-border rounded-lg p-3 bg-accent/30">
                <div className="flex items-start justify-between mb-2">
                  <div className="text-card-foreground">{activity.tenant}</div>
                  <div className="text-success">EGP {activity.amount.toLocaleString()}</div>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <Tag variant="success">Paid ({activity.method})</Tag>
                  <span className="text-muted-foreground">{activity.time}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Top Tenants */}
        <div className="bg-card border border-border rounded-lg p-6">
          <div className="flex items-center gap-2 mb-4">
            <TrendingUp className="text-primary" size={20} />
            <h3 className="text-card-foreground">Top Tenants (By Revenue)</h3>
          </div>

          <div className="space-y-4">
            {topTenants.map((tenant, index) => (
              <div key={tenant.name} className="flex items-center gap-4">
                <div className="w-8 h-8 bg-primary/20 rounded-full flex items-center justify-center text-primary">
                  {index + 1}
                </div>
                <div className="flex-1">
                  <div className="text-card-foreground">{tenant.name}</div>
                  <div className="text-sm text-muted-foreground">
                    EGP {tenant.revenue.toLocaleString()}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
