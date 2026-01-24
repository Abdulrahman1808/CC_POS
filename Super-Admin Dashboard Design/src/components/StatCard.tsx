import React from 'react';

interface StatCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  trend?: {
    value: string;
    isPositive: boolean;
  };
}

export function StatCard({ title, value, icon, trend }: StatCardProps) {
  return (
    <div className="bg-card border border-border rounded-lg p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="text-muted-foreground">{title}</div>
        <div className="text-primary">{icon}</div>
      </div>
      <div className="text-3xl text-card-foreground mb-2">{value}</div>
      {trend && (
        <div className={`text-sm ${trend.isPositive ? 'text-success' : 'text-destructive'}`}>
          {trend.isPositive ? '↑' : '↓'} {trend.value}
        </div>
      )}
    </div>
  );
}
