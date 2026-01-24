import React from 'react';
import { ChevronLeft, ChevronRight, MoreVertical } from 'lucide-react';
import { Tag } from './Tag';

interface Column {
  key: string;
  label: string;
  render?: (value: any, row: any) => React.ReactNode;
}

interface DataTableProps {
  columns: Column[];
  data: any[];
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onAction?: (action: string, row: any) => void;
}

export function DataTable({ columns, data, currentPage, totalPages, onPageChange, onAction }: DataTableProps) {
  const [openDropdown, setOpenDropdown] = React.useState<number | null>(null);
  
  return (
    <div className="bg-card border border-border rounded-lg overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-muted/50 border-b border-border">
            <tr>
              {columns.map((column) => (
                <th key={column.key} className="px-6 py-3 text-left text-muted-foreground">
                  {column.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map((row, index) => (
              <tr key={row.id || index} className="border-b border-border hover:bg-accent/50 transition-colors">
                {columns.map((column) => (
                  <td key={column.key} className="px-6 py-4 text-card-foreground">
                    {column.render ? column.render(row[column.key], row) : row[column.key]}
                  </td>
                ))}
                {onAction && (
                  <td className="px-6 py-4">
                    <div className="relative">
                      <button
                        onClick={() => setOpenDropdown(openDropdown === index ? null : index)}
                        className="p-1 hover:bg-accent rounded transition-colors"
                      >
                        <MoreVertical size={16} />
                      </button>
                      
                      {openDropdown === index && (
                        <div className="absolute right-0 mt-2 w-48 bg-popover border border-border rounded-lg shadow-lg py-1 z-50">
                          <button
                            onClick={() => {
                              onAction('view', row);
                              setOpenDropdown(null);
                            }}
                            className="w-full text-left px-4 py-2 text-popover-foreground hover:bg-accent transition-colors"
                          >
                            View Details
                          </button>
                          <button
                            onClick={() => {
                              onAction('edit', row);
                              setOpenDropdown(null);
                            }}
                            className="w-full text-left px-4 py-2 text-popover-foreground hover:bg-accent transition-colors"
                          >
                            Edit Tenant
                          </button>
                          <button
                            onClick={() => {
                              onAction('deactivate', row);
                              setOpenDropdown(null);
                            }}
                            className="w-full text-left px-4 py-2 text-destructive hover:bg-accent transition-colors"
                          >
                            Deactivate Tenant
                          </button>
                        </div>
                      )}
                    </div>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      
      <div className="px-6 py-4 border-t border-border flex items-center justify-between">
        <div className="text-muted-foreground">
          Page {currentPage} of {totalPages}
        </div>
        
        <div className="flex items-center gap-2">
          <button
            onClick={() => onPageChange(currentPage - 1)}
            disabled={currentPage === 1}
            className="p-2 rounded-lg border border-border hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <ChevronLeft size={16} />
          </button>
          <button
            onClick={() => onPageChange(currentPage + 1)}
            disabled={currentPage === totalPages}
            className="p-2 rounded-lg border border-border hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <ChevronRight size={16} />
          </button>
        </div>
      </div>
    </div>
  );
}
