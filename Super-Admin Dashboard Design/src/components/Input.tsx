import React from 'react';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
}

export function Input({ label, className = '', ...props }: InputProps) {
  return (
    <div className="flex flex-col gap-2">
      {label && <label className="text-foreground">{label}</label>}
      <input
        className={`bg-input border border-border text-foreground px-4 py-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-ring ${className}`}
        {...props}
      />
    </div>
  );
}
