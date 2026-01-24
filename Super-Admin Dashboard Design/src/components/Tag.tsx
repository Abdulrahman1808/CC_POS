import React from 'react';

interface TagProps {
  children: React.ReactNode;
  variant?: 'success' | 'warning' | 'error' | 'default';
}

export function Tag({ children, variant = 'default' }: TagProps) {
  const variantStyles = {
    success: 'bg-success/20 text-success border-success/30',
    warning: 'bg-warning/20 text-warning border-warning/30',
    error: 'bg-destructive/20 text-destructive border-destructive/30',
    default: 'bg-muted text-muted-foreground border-border'
  };
  
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-md border text-sm ${variantStyles[variant]}`}>
      {children}
    </span>
  );
}
