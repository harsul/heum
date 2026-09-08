import { useAuth } from 'react-oidc-context';
import Grid from '@mui/material/Grid';
import Skeleton from '@mui/material/Skeleton';
import BusinessIcon from '@mui/icons-material/BusinessOutlined';
import CheckCircleIcon from '@mui/icons-material/CheckCircleOutlined';
import LayersIcon from '@mui/icons-material/LayersOutlined';
import TuneIcon from '@mui/icons-material/TuneOutlined';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { AppWidgetSummary } from '../components/widgets/AppWidgetSummary';
import { RecentTenantsCard } from '../features/dashboard/components/RecentTenantsCard';
import { QuickLinksCard } from '../features/dashboard/components/QuickLinksCard';
import { useAdminStats } from '../features/dashboard/hooks/useAdminStats';
import { isSystemAdmin } from '../auth/roles';

function StatSkeleton() {
  return <Skeleton variant="rounded" height={104} />;
}

export function DashboardPage() {
  const { user } = useAuth();
  const sysAdmin = isSystemAdmin(user);
  const { data: stats, isLoading } = useAdminStats();

  if (!sysAdmin) {
    return (
      <DashboardLayout>
        <Grid container spacing={3}>
          <Grid size={{ xs: 12 }}>
            <AppWidgetSummary
              title="Welcome to Heum"
              total="Your workspace is ready"
              icon={BusinessIcon}
              color="primary"
            />
          </Grid>
        </Grid>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout>
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          {isLoading ? (
            <StatSkeleton />
          ) : (
            <AppWidgetSummary
              title="Total Tenants"
              total={stats?.totalTenants ?? 0}
              icon={BusinessIcon}
              color="primary"
            />
          )}
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          {isLoading ? (
            <StatSkeleton />
          ) : (
            <AppWidgetSummary
              title="Active Tenants"
              total={stats?.activeTenants ?? 0}
              icon={CheckCircleIcon}
              color="success"
            />
          )}
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          {isLoading ? (
            <StatSkeleton />
          ) : (
            <AppWidgetSummary
              title="Plans"
              total={stats?.totalPlans ?? 0}
              icon={LayersIcon}
              color="secondary"
            />
          )}
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          {isLoading ? (
            <StatSkeleton />
          ) : (
            <AppWidgetSummary
              title="Entitlements"
              total={stats?.totalEntitlements ?? 0}
              icon={TuneIcon}
              color="warning"
            />
          )}
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 8 }}>
          <RecentTenantsCard tenants={stats?.recentTenants} loading={isLoading} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <QuickLinksCard />
        </Grid>
      </Grid>
    </DashboardLayout>
  );
}
