import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import CircularProgress from '@mui/material/CircularProgress';
import Grid from '@mui/material/Grid';
import Stack from '@mui/material/Stack';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import Typography from '@mui/material/Typography';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { DetailField } from '../components/DetailField';
import { CompanyUsersTable } from '../features/company/components/CompanyUsersTable';
import { useMyTenant } from '../features/company/hooks/useMyTenant';
import { formatDate, tenantInitials } from '../utils/format';

type TabValue = 'overview' | 'users';

export function MyCompanyPage() {
  const { data: tenant, isLoading, isError } = useMyTenant();
  const [activeTab, setActiveTab] = useState<TabValue>('overview');

  return (
    <DashboardLayout>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          My Company
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Overview of your company and its users.
        </Typography>
      </Box>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
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
                <Avatar sx={{ width: 56, height: 56, bgcolor: 'primary.main', fontSize: 20 }}>
                  {tenantInitials(tenant.name)}
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
                color={tenant.isActive ? 'success' : 'default'}
                variant={tenant.isActive ? 'filled' : 'outlined'}
              />
            </Stack>

            <Tabs
              value={activeTab}
              onChange={(_, value: TabValue) => setActiveTab(value)}
              sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}
            >
              <Tab label="Overview" value="overview" />
              <Tab label="Users" value="users" />
            </Tabs>

            {activeTab === 'overview' && (
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
            )}

            {activeTab === 'users' && <CompanyUsersTable />}
          </CardContent>
        </Card>
      )}
    </DashboardLayout>
  );
}
