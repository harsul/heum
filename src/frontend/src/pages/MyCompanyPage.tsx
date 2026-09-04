import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import Grid from '@mui/material/Grid';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import Box from '@mui/material/Box';
import { Typography } from '@mui/material';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { DetailField } from '../components/DetailField';
import { CompanySettingsPanel } from '../features/company/components/CompanySettingsPanel';
import { CompanyUsersTable } from '../features/company/components/CompanyUsersTable';
import { TenantLogoPanel } from '../features/company/components/TenantLogoPanel';
import { SubscriptionTabContent } from '../features/plans/components/SubscriptionTabContent';
import { useMyTenant } from '../features/company/hooks/useMyTenant';
import { useMyPlan } from '../features/company/hooks/useMyPlan';
import { formatDate, tenantInitials } from '../utils/format';

type TabValue = 'overview' | 'users' | 'settings' | 'subscription';

export function MyCompanyPage() {
  const { data: tenant, isLoading, isError } = useMyTenant();
  const [activeTab, setActiveTab] = useState<TabValue>('overview');

  const { data: myPlan, isLoading: planLoading } = useMyPlan();

  return (
    <DashboardLayout>
      {isLoading && (
        <Card>
          <CardContent sx={{ p: 4 }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={3}
              sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between', mb: 3 }}
            >
              <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                <Skeleton variant="circular" width={56} height={56} />
                <Stack>
                  <Skeleton variant="text" width={160} sx={{ fontSize: '1.5rem' }} />
                  <Skeleton variant="text" width={100} sx={{ fontSize: '0.875rem' }} />
                </Stack>
              </Stack>
              <Skeleton variant="rounded" width={64} height={24} />
            </Stack>
            <Skeleton variant="rectangular" height={48} sx={{ mb: 3, borderRadius: 1 }} />
            <Grid container spacing={3}>
              {Array.from({ length: 4 }, (_, i) => (
                <Grid key={i} size={{ xs: 12, sm: 6, md: 3 }}>
                  <Skeleton variant="text" width={80} sx={{ fontSize: '0.75rem' }} />
                  <Skeleton variant="text" width={120} />
                </Grid>
              ))}
            </Grid>
          </CardContent>
        </Card>
      )}

      {isError && <Alert severity="error">Failed to load your company. Please try again.</Alert>}

      {tenant && (
        <Card>
          <CardContent sx={{ p: 4 }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={3}
              sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between', mb: 3 }}
            >
              <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                <Avatar
                  src={tenant.logoUrl ?? undefined}
                  sx={{ width: 56, height: 56, bgcolor: 'primary.main', fontSize: 20 }}
                >
                  {!tenant.logoUrl && tenantInitials(tenant.name)}
                </Avatar>
                <Box>
                  <Typography variant="h5" sx={{ fontWeight: 700 }}>
                    {tenant.name}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {tenant.slug}
                  </Typography>
                </Box>
              </Stack>

              <Chip
                size="small"
                label={tenant.isActive ? 'Active' : 'Inactive'}
                color={tenant.isActive ? 'success' : 'warning'}
                variant='filled'
              />
            </Stack>

            <Tabs
              value={activeTab}
              onChange={(_, value: TabValue) => setActiveTab(value)}
              sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}
            >
              <Tab label="Overview" value="overview" />
              <Tab label="Users" value="users" />
              <Tab label="Settings" value="settings" />
              <Tab label="Subscription" value="subscription" />
            </Tabs>

            {activeTab === 'overview' && (
              <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                <Grid container spacing={3}>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <DetailField label="Company ID" value={tenant.id} />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <DetailField label="Name" value={tenant.name} />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <DetailField label="Created" value={formatDate(tenant.createdAtUtc)} />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <DetailField label="Last updated" value={formatDate(tenant.updatedAtUtc)} />
                  </Grid>
                </Grid>
              </Paper>
            )}

            {activeTab === 'users' && (
              <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                <CompanyUsersTable />
              </Paper>
            )}

            {activeTab === 'settings' && (
              <Stack spacing={2}>
                <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                  <TenantLogoPanel tenant={tenant} />
                </Paper>
                <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                  <CompanySettingsPanel />
                </Paper>
              </Stack>
            )}

            {activeTab === 'subscription' && (
              <SubscriptionTabContent
                currentPlan={myPlan ?? null}
                isLoading={planLoading}
              />
            )}
          </CardContent>
        </Card>
      )}
    </DashboardLayout>
  );
}
