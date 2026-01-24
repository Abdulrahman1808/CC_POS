import React, { useState } from 'react';
import { 
  TrendingUp, 
  TrendingDown, 
  DollarSign, 
  Users, 
  ShoppingCart, 
  Calendar,
  Download,
  Filter
} from 'lucide-react';
import { StatCard } from './StatCard';
import { LineChart } from './LineChart';
import { BarChart } from './BarChart';
import { Button } from './Button';

export function Analytics() {
  const [dateRange, setDateRange] = useState('30d');
  const [selectedMetric, setSelectedMetric] = useState('revenue');
  
  // Revenue comparison data
  const revenueComparison = [
    { period: 'Week 1', current: 95000, previous: 82000 },
    { period: 'Week 2', current: 102000, previous: 88000 },
    { period: 'Week 3', current: 98000, previous: 91000 },
    { period: 'Week 4', current: 110000, previous: 95000 }
  ];
  
  // Tenant activity by plan
  const planBreakdown = [
    { plan: 'Basic', count: 58, revenue: 145000 },
    { plan: 'Pro', count: 42, revenue: 210000 },
    { plan: 'Enterprise', count: 15, revenue: 375000 }
  ];
  
  // Payment method distribution
  const paymentMethods = [
    { method: 'Fawry', transactions: 3420, percentage: 45 },
    { method: 'Cash', transactions: 2890, percentage: 38 },
    { method: 'Card', transactions: 1290, percentage: 17 }
  ];
  
  // Top performing regions (mock data)
  const regionData = [
    { region: 'Cairo', tenants: 45, revenue: 320000, growth: 12 },
    { region: 'Alexandria', tenants: 28, revenue: 185000, growth: 8 },
    { region: 'Giza', tenants: 22, revenue: 145000, growth: 15 },
    { region: 'Mansoura', tenants: 15, revenue: 98000, growth: -3 },
    { region: 'Aswan', tenants: 12, revenue: 76000, growth: 22 }
  ];
  
  // Hourly transaction volume
  const hourlyData = [
    { hour: '00:00', transactions: 45 },
    { hour: '03:00', transactions: 12 },
    { hour: '06:00', transactions: 89 },
    { hour: '09:00', transactions: 234 },
    { hour: '12:00', transactions: 456 },
    { hour: '15:00', transactions: 389 },
    { hour: '18:00', transactions: 512 },
    { hour: '21:00', transactions: 298 }
  ];
  
  const handleExportData = () => {
    console.log('Exporting analytics data...');
    // In production, generate CSV/Excel export
  };
  
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-foreground mb-1">Advanced Analytics</h1>
          <p className="text-muted-foreground">Deep insights into platform performance and tenant behavior</p>
        </div>
        <div className="flex gap-3">
          <select
            value={dateRange}
            onChange={(e) => setDateRange(e.target.value)}
            className="bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="7d">Last 7 Days</option>
            <option value="30d">Last 30 Days</option>
            <option value="90d">Last 90 Days</option>
            <option value="1y">Last Year</option>
          </select>
          <Button onClick={handleExportData}>
            <Download size={18} className="mr-2" />
            Export Report
          </Button>
        </div>
      </div>
      
      {/* Key Performance Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Total Revenue (30d)"
          value="EGP 1,405,000"
          icon={<DollarSign size={24} />}
          trend={{ value: '18.5% vs last period', isPositive: true }}
        />
        <StatCard
          title="Active Tenants"
          value="1,142"
          icon={<Users size={24} />}
          trend={{ value: '8.2% vs last period', isPositive: true }}
        />
        <StatCard
          title="Total Transactions"
          value="7,600"
          icon={<ShoppingCart size={24} />}
          trend={{ value: '12.1% vs last period', isPositive: true }}
        />
        <StatCard
          title="Avg Revenue per Tenant"
          value="EGP 1,230"
          icon={<TrendingUp size={24} />}
          trend={{ value: '5.3% vs last period', isPositive: true }}
        />
      </div>
      
      {/* Revenue Trend Comparison */}
      <div className="bg-card border border-border rounded-lg p-6">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-card-foreground">Revenue Trend Comparison</h3>
          <div className="flex gap-4 text-sm">
            <div className="flex items-center gap-2">
              <div className="w-3 h-3 bg-primary rounded-full"></div>
              <span className="text-muted-foreground">Current Period</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-3 h-3 bg-muted-foreground/40 rounded-full"></div>
              <span className="text-muted-foreground">Previous Period</span>
            </div>
          </div>
        </div>
        <div className="h-80">
          <LineChart
            data={revenueComparison}
            dataKey="current"
            xAxisKey="period"
            title=""
            showSecondLine={true}
            secondDataKey="previous"
          />
        </div>
      </div>
      
      {/* Plan Breakdown & Payment Methods */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Subscription Plan Breakdown */}
        <div className="bg-card border border-border rounded-lg p-6">
          <h3 className="text-card-foreground mb-6">Subscription Plan Distribution</h3>
          <div className="space-y-4">
            {planBreakdown.map((plan) => {
              const total = planBreakdown.reduce((sum, p) => sum + p.count, 0);
              const percentage = ((plan.count / total) * 100).toFixed(1);
              
              return (
                <div key={plan.plan} className="space-y-2">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-card-foreground">{plan.plan}</span>
                    <span className="text-muted-foreground">{plan.count} tenants ({percentage}%)</span>
                  </div>
                  <div className="w-full bg-muted rounded-full h-2">
                    <div 
                      className="bg-primary rounded-full h-2 transition-all duration-500"
                      style={{ width: `${percentage}%` }}
                    ></div>
                  </div>
                  <div className="text-sm text-success">
                    EGP {plan.revenue.toLocaleString()} total revenue
                  </div>
                </div>
              );
            })}
          </div>
        </div>
        
        {/* Payment Method Distribution */}
        <div className="bg-card border border-border rounded-lg p-6">
          <h3 className="text-card-foreground mb-6">Payment Method Usage</h3>
          <div className="space-y-6">
            {paymentMethods.map((method) => (
              <div key={method.method} className="space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-primary/20 rounded-lg flex items-center justify-center">
                      <ShoppingCart className="text-primary" size={20} />
                    </div>
                    <div>
                      <div className="text-card-foreground">{method.method}</div>
                      <div className="text-sm text-muted-foreground">
                        {method.transactions.toLocaleString()} transactions
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-lg text-primary">{method.percentage}%</div>
                  </div>
                </div>
                <div className="w-full bg-muted rounded-full h-2">
                  <div 
                    className="bg-success rounded-full h-2 transition-all duration-500"
                    style={{ width: `${method.percentage}%` }}
                  ></div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
      
      {/* Regional Performance */}
      <div className="bg-card border border-border rounded-lg p-6">
        <h3 className="text-card-foreground mb-6">Regional Performance</h3>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left py-3 px-4 text-muted-foreground">Region</th>
                <th className="text-left py-3 px-4 text-muted-foreground">Active Tenants</th>
                <th className="text-left py-3 px-4 text-muted-foreground">Total Revenue</th>
                <th className="text-left py-3 px-4 text-muted-foreground">Growth Rate</th>
              </tr>
            </thead>
            <tbody>
              {regionData.map((region) => (
                <tr key={region.region} className="border-b border-border/50 hover:bg-accent/30 transition-colors">
                  <td className="py-3 px-4 text-card-foreground">{region.region}</td>
                  <td className="py-3 px-4 text-card-foreground">{region.tenants}</td>
                  <td className="py-3 px-4 text-success">EGP {region.revenue.toLocaleString()}</td>
                  <td className="py-3 px-4">
                    <div className={`flex items-center gap-1 ${region.growth >= 0 ? 'text-success' : 'text-destructive'}`}>
                      {region.growth >= 0 ? <TrendingUp size={16} /> : <TrendingDown size={16} />}
                      <span>{Math.abs(region.growth)}%</span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      
      {/* Hourly Transaction Volume */}
      <div className="bg-card border border-border rounded-lg p-6">
        <h3 className="text-card-foreground mb-6">Transaction Volume by Hour</h3>
        <BarChart
          data={hourlyData}
          dataKey="transactions"
          xAxisKey="hour"
          title=""
        />
      </div>
    </div>
  );
}
