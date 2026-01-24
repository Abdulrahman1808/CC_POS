import React, { useState } from 'react';
import { Plus, Search } from 'lucide-react';
import { Button } from './Button';
import { DataTable } from './DataTable';
import { Tag } from './Tag';
import { Modal } from './Modal';
import { Input } from './Input';

export function TenantManagement() {
  const [currentPage, setCurrentPage] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [planFilter, setPlanFilter] = useState('all');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newTenant, setNewTenant] = useState({
    businessName: '',
    adminName: '',
    adminEmail: '',
    adminPassword: '',
    plan: 'basic'
  });
  
  // Sample tenant data (in production, this would come from Supabase)
  const allTenants = Array.from({ length: 142 }, (_, i) => ({
    id: i + 1,
    name: `Business ${i + 1}`,
    email: `admin${i + 1}@business.com`,
    status: ['active', 'inactive', 'trial'][Math.floor(Math.random() * 3)],
    plan: ['Basic', 'Pro', 'Enterprise'][Math.floor(Math.random() * 3)],
    dateJoined: new Date(2024, Math.floor(Math.random() * 12), Math.floor(Math.random() * 28) + 1).toLocaleDateString('en-GB')
  }));
  
  // Filter tenants
  const filteredTenants = allTenants.filter(tenant => {
    const matchesSearch = tenant.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         tenant.email.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesStatus = statusFilter === 'all' || tenant.status === statusFilter;
    const matchesPlan = planFilter === 'all' || tenant.plan.toLowerCase() === planFilter.toLowerCase();
    return matchesSearch && matchesStatus && matchesPlan;
  });
  
  // Pagination
  const itemsPerPage = 10;
  const totalPages = Math.ceil(filteredTenants.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const paginatedTenants = filteredTenants.slice(startIndex, startIndex + itemsPerPage);
  
  const columns = [
    { key: 'name', label: 'Tenant Name' },
    { key: 'email', label: 'Admin Email' },
    {
      key: 'status',
      label: 'Status',
      render: (status: string) => {
        const variant = status === 'active' ? 'success' : status === 'trial' ? 'warning' : 'default';
        return <Tag variant={variant}>{status.charAt(0).toUpperCase() + status.slice(1)}</Tag>;
      }
    },
    { key: 'plan', label: 'Subscription Plan' },
    { key: 'dateJoined', label: 'Date Joined' }
  ];
  
  const handleAction = (action: string, row: any) => {
    console.log(`Action: ${action}`, row);
    // In production, handle these actions with Supabase
  };
  
  const handleCreateTenant = (e: React.FormEvent) => {
    e.preventDefault();
    console.log('Creating tenant:', newTenant);
    // In production, create tenant in Supabase
    setShowCreateModal(false);
    setNewTenant({
      businessName: '',
      adminName: '',
      adminEmail: '',
      adminPassword: '',
      plan: 'basic'
    });
  };
  
  const generatePassword = () => {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*';
    let password = '';
    for (let i = 0; i < 12; i++) {
      password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    setNewTenant({ ...newTenant, adminPassword: password });
  };
  
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-foreground">Tenant Management</h1>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus size={20} className="mr-2" />
          Create New Tenant
        </Button>
      </div>
      
      {/* Filters */}
      <div className="bg-card border border-border rounded-lg p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={20} />
            <input
              type="text"
              placeholder="Search tenants..."
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value);
                setCurrentPage(1);
              }}
              className="w-full bg-input border border-border rounded-lg pl-10 pr-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setCurrentPage(1);
            }}
            className="bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
            <option value="trial">Trial</option>
          </select>
          
          <select
            value={planFilter}
            onChange={(e) => {
              setPlanFilter(e.target.value);
              setCurrentPage(1);
            }}
            className="bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="all">All Plans</option>
            <option value="basic">Basic</option>
            <option value="pro">Pro</option>
            <option value="enterprise">Enterprise</option>
          </select>
        </div>
      </div>
      
      {/* Table */}
      <DataTable
        columns={columns}
        data={paginatedTenants}
        currentPage={currentPage}
        totalPages={totalPages}
        onPageChange={setCurrentPage}
        onAction={handleAction}
      />
      
      {/* Create Tenant Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        title="Onboard New Tenant"
      >
        <form onSubmit={handleCreateTenant} className="space-y-4">
          <Input
            label="Business Name"
            placeholder="e.g., Bite & Brew Cafe"
            value={newTenant.businessName}
            onChange={(e) => setNewTenant({ ...newTenant, businessName: e.target.value })}
            required
          />
          
          <Input
            label="Admin Full Name"
            placeholder="e.g., John Smith"
            value={newTenant.adminName}
            onChange={(e) => setNewTenant({ ...newTenant, adminName: e.target.value })}
            required
          />
          
          <Input
            label="Admin Email Address"
            type="email"
            placeholder="admin@business.com"
            value={newTenant.adminEmail}
            onChange={(e) => setNewTenant({ ...newTenant, adminEmail: e.target.value })}
            required
          />
          
          <div>
            <label className="text-foreground mb-2 block">Admin Password</label>
            <div className="flex gap-2">
              <Input
                type="text"
                placeholder="Generate or enter password"
                value={newTenant.adminPassword}
                onChange={(e) => setNewTenant({ ...newTenant, adminPassword: e.target.value })}
                required
              />
              <Button type="button" variant="secondary" onClick={generatePassword}>
                Generate
              </Button>
            </div>
          </div>
          
          <div>
            <label className="text-foreground mb-2 block">Subscription Plan</label>
            <select
              value={newTenant.plan}
              onChange={(e) => setNewTenant({ ...newTenant, plan: e.target.value })}
              className="w-full bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="basic">Basic</option>
              <option value="pro">Pro</option>
              <option value="enterprise">Enterprise</option>
            </select>
          </div>
          
          <div className="flex gap-2 pt-4">
            <Button type="button" variant="secondary" onClick={() => setShowCreateModal(false)} className="flex-1">
              Cancel
            </Button>
            <Button type="submit" className="flex-1">
              Create Tenant & Send Invite
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
