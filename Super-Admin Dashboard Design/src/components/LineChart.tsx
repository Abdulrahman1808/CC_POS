import React from 'react';
import { LineChart as RechartsLineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface LineChartProps {
  data: any[];
  dataKey: string;
  xAxisKey: string;
  title?: string;
  showSecondLine?: boolean;
  secondDataKey?: string;
}

export function LineChart({ data, dataKey, xAxisKey, title, showSecondLine, secondDataKey }: LineChartProps) {
  return (
    <div className="bg-card border border-border rounded-lg p-6">
      {title && <h3 className="text-card-foreground mb-4">{title}</h3>}
      <ResponsiveContainer width="100%" height={300}>
        <RechartsLineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
          <XAxis 
            dataKey={xAxisKey} 
            stroke="#9CA3AF"
            style={{ fontSize: '12px' }}
          />
          <YAxis 
            stroke="#9CA3AF"
            style={{ fontSize: '12px' }}
          />
          <Tooltip 
            contentStyle={{ 
              backgroundColor: '#1F2937', 
              border: '1px solid #374151',
              borderRadius: '8px',
              color: '#F9FAFB'
            }}
          />
          <Line 
            type="monotone" 
            dataKey={dataKey} 
            stroke="#3B82F6" 
            strokeWidth={2}
            dot={{ fill: '#3B82F6', r: 4 }}
          />
          {showSecondLine && secondDataKey && (
            <Line 
              type="monotone" 
              dataKey={secondDataKey} 
              stroke="#9CA3AF" 
              strokeWidth={2}
              strokeDasharray="5 5"
              dot={{ fill: '#9CA3AF', r: 4 }}
            />
          )}
        </RechartsLineChart>
      </ResponsiveContainer>
    </div>
  );
}