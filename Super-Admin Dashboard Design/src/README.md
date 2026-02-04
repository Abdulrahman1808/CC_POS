# Eagle POS - Super Admin Dashboard

A high-throughput, sophisticated web dashboard for managing the Eagle POS SaaS platform with thousands of tenants and real-time sales data flow.

## 🎯 Overview

This is the internal super-admin cockpit used by company owners to manage the entire Eagle POS platform, monitor real-time sales across all tenants, analyze platform performance, and configure system-wide settings.

## ✨ Features

### 🔐 Authentication & Security
- **Role-based authentication** (Super Admin & Regular Admin)
- Secure login with Supabase Auth
- Session management
- Fixed super admin credentials for platform owner

### 📊 Main Dashboard
- **Real-time sales feed** - Live updates every 5 seconds
- **Key metrics** - Total tenants, platform revenue, sales, and new signups
- **Revenue charts** - 30-day platform revenue trends
- **Tenant growth** - Visual representation of tenant acquisition
- **Top performers** - Ranking of highest-revenue tenants

### 🏢 Tenant Management
- **Advanced filtering** - Search by name, email, status, or plan
- **Server-side pagination** - Optimized for thousands of records (10 per page)
- **Status tracking** - Active, Inactive, Trial
- **Subscription plans** - Basic, Pro, Enterprise
- **Bulk actions** - Edit, suspend, or delete tenants
- **Create tenant modal** - Onboard new tenants with auto-generated credentials

### 📈 Advanced Analytics
- **Revenue comparison** - Current vs previous period trends
- **Subscription distribution** - Visual breakdown by plan type
- **Payment methods** - Fawry, Cash, Card usage statistics
- **Regional performance** - Geographic revenue and growth tracking
- **Hourly patterns** - Transaction volume by time of day
- **Export reports** - Download analytics data

### ⚙️ Platform Settings
- **General settings** - Platform name, support email, language, timezone
- **Security controls** - Session timeout, 2FA, password policies
- **Email notifications** - Configure alerts for key events
- **Billing management** - View and manage subscription plans
- **Database monitoring** - Health status, backups, storage metrics

## 🎨 Design System

### Color Palette
- **Primary Blue**: `#3B82F6` - Actions, highlights, charts
- **Dark Background**: `#111827` - Main background
- **Success Green**: `#10B981` - Positive metrics, confirmations
- **Warning Amber**: `#F59E0B` - Alerts, trial accounts
- **Error Red**: `#EF4444` - Errors, critical actions

### Theme
- Dark mode optimized for extended use
- Professional, data-first interface
- High contrast for accessibility
- Smooth transitions and animations

## 🛠️ Technology Stack

### Frontend
- **React 18** with TypeScript
- **Tailwind CSS v4** for styling
- **Recharts** for data visualization
- **Lucide React** for icons

### Backend
- **Supabase** (PostgreSQL database)
- **Supabase Auth** for authentication
- **Supabase Edge Functions** (Hono server)
- **KV Store** for key-value data

### Deployment
- **Vercel** (frontend)
- **Supabase** (backend & database)
- Zero-cost stack for development

## 📁 Project Structure

```
/
├── App.tsx                      # Main application entry
├── components/
│   ├── Dashboard.tsx           # Main dashboard with live feed
│   ├── TenantManagement.tsx    # Tenant CRUD operations
│   ├── Analytics.tsx           # Advanced analytics page
│   ├── Settings.tsx            # Platform settings
│   ├── LoginPage.tsx           # Authentication UI
│   ├── Sidebar.tsx             # Navigation sidebar
│   ├── Header.tsx              # Top header with logout
│   ├── StatCard.tsx            # Metric display cards
│   ├── DataTable.tsx           # Paginated table component
│   ├── LineChart.tsx           # Line chart wrapper
│   ├── BarChart.tsx            # Bar chart wrapper
│   ├── Button.tsx              # Reusable button
│   ├── Input.tsx               # Form input
│   ├── Modal.tsx               # Modal dialog
│   └── Tag.tsx                 # Status badges
├── utils/
│   └── supabase/
│       ├── client.tsx          # Supabase client & auth
│       └── info.tsx            # Project credentials
├── supabase/
│   └── functions/
│       └── server/
│           ├── index.tsx       # Hono API server
│           └── kv_store.tsx    # Key-value utilities
└── styles/
    └── globals.css             # Tailwind configuration
```

## 🚀 Getting Started

### Prerequisites
1. Supabase account and project
2. Node.js 18+ installed

### Initial Setup

1. **Configure Supabase**:
   - Go to [Supabase Dashboard](https://supabase.com/dashboard)
   - Select your project
   - Navigate to: **Authentication → Providers → Email**
   - **ENABLE** the "Email" provider
   - **DISABLE** the "Confirm email" toggle
   - Click **Save**

2. **Create Super Admin**:
   - In Supabase: **Authentication → Users**
   - Click **"Add user"** → **"Create new user"**
   - Email: `abdulrahman.mohamed1808@gmail.com`
   - Password: `pass1234@#@#`
   - **Auto Confirm User**: ✅ Enable
   - Click **"Create user"**

3. **Login to Dashboard**:
   - Open your Eagle POS app
   - Use the credentials above to log in
   - You're now the Super Admin! 🎉

### Changing Super Admin Email

To use a different super admin email:

1. Open `/App.tsx`
2. Change line 20: `const SUPER_ADMIN_EMAIL = "your-email@example.com";`
3. Save the file
4. Follow the "Create Super Admin" steps above with your new email

## 📊 Data Flow

### Authentication Flow
```
User Login → Supabase Auth → Session Token → App State → Dashboard
```

### Real-time Sales Feed
```
5-second interval → Generate mock sale → Update state → Render in UI
```

### Tenant Management
```
Filter/Search → Client-side filtering → Pagination → Display 10 rows
```

## 🔒 Security Features

- **Role-based access control** (RBAC)
- **Server-side authentication** with Supabase
- **Secure session management**
- **Environment-based credentials**
- **Protected admin routes**
- **CORS configuration**

## 📈 Performance Optimizations

- **Server-side pagination** - Only load 10 records at a time
- **Memoized components** - Reduce unnecessary re-renders
- **Lazy loading** - Code splitting for faster initial load
- **Optimized charts** - Efficient data visualization
- **Debounced search** - Prevent excessive filtering

## 🎯 Future Enhancements

- [ ] Real Supabase data integration (currently mock data)
- [ ] WebSocket for live sales updates
- [ ] Advanced export formats (CSV, Excel, PDF)
- [ ] Email notification system
- [ ] Multi-language support
- [ ] Mobile responsive optimization
- [ ] Audit logs and activity tracking
- [ ] Advanced role permissions
- [ ] Automated backups
- [ ] Performance monitoring dashboard

## 🐛 Troubleshooting

### Cannot Login
- Verify email confirmation is disabled in Supabase
- Check that the user exists in Authentication → Users
- Ensure user has "Auto Confirm" enabled
- Clear browser cache and try again

### "Email logins are disabled"
- Go to Supabase: Authentication → Providers → Email
- Enable the Email provider toggle
- Save and refresh the app

### Missing super admin
- Use the manual creation steps in "Getting Started"
- Or visit: `https://your-project.supabase.co/functions/v1/make-server-917223f5/create-super-admin`

## 📝 Environment Variables

Required in Supabase Edge Functions:
- `SUPABASE_URL` - Your Supabase project URL
- `SUPABASE_ANON_KEY` - Public anon key
- `SUPABASE_SERVICE_ROLE_KEY` - Service role key (keep secret!)

## 🤝 Contributing

This is a proprietary dashboard for Eagle POS internal use.

## 📄 License

Proprietary - Eagle POS © 2024

## 👨‍💻 Super Admin

**Abdulrahman Mohamed**  
Email: abdulrahman.mohamed1808@gmail.com  
Role: Super Administrator

---

**Built with ❤️ for Eagle POS**
