import { useState } from 'react';
import { useParams } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import Skeleton from '@mui/material/Skeleton';
import Grid from '@mui/material/Grid';
import Paper from '@mui/material/Paper';
import Stack from '@mui/material/Stack';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import Typography from '@mui/material/Typography';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { DetailField } from '../components/DetailField';
import { EditTenantDialog } from '../features/tenants/components/EditTenantDialog';
import { TenantHistoryTable } from '../features/tenants/components';
import { TenantSettingsPanel } from '../features/tenants/components';
import { TenantUsersTable } from '../features/tenants/components/TenantUsersTable';
import { AssignPlanDialog } from '../features/plans/components/AssignPlanDialog';
import { SubscriptionTabContent } from '../features/plans/components/SubscriptionTabContent';
import { useCurrentSubscription, useSubscriptionHistory, useResolvedEntitlements } from '../features/plans/hooks/useTenantSubscription';
import { useAssignPlan } from '../features/plans/hooks/useSubscriptionMutations';
import { usePlan, useEntitlements } from '../features/plans/hooks/usePlans';
import { useTenant } from '../features/tenants/hooks/useTenant';
import { useSetTenantActive, useUpdateTenant } from '../features/tenants/hooks/useTenantMutations';
import { formatDate, tenantInitials } from '../utils/format';
import { getApiErrorMessage } from '../utils/apiError';
import { enqueueSnackbar } from 'notistack';

type TabValue = 'overview' | 'users' | 'history' | 'settings' | 'subscription';

export function TenantDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: tenant, isLoading, isError } = useTenant(id);
  const updateTenant = useUpdateTenant();
  const setTenantActive = useSetTenantActive();
  const [isEditing, setIsEditing] = useState(false);
  const [activeTab, setActiveTab] = useState<TabValue>('overview');
  const [isAssigningPlan, setIsAssigningPlan] = useState(false);

  const activeOnSub = activeTab === 'subscription' ? id : undefined;
  const { data: currentSub, isLoading: subLoading } = useCurrentSubscription(activeOnSub);
  const { data: subHistory = [], isLoading: historyLoading } = useSubscriptionHistory(activeOnSub);
  const { data: entitlements = {}, isLoading: entitlementsLoading } = useResolvedEntitlements(activeOnSub);
  const { data: planDetails } = usePlan(activeOnSub && currentSub?.planId ? currentSub.planId : undefined);
  const { data: catalogEntitlements = [] } = useEntitlements();
  const assignPlan = useAssignPlan(id ?? '');

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
                <Box>
                  <Skeleton variant="text" width={160} sx={{ fontSize: '1.5rem' }} />
                  <Skeleton variant="text" width={100} sx={{ fontSize: '0.875rem' }} />
                </Box>
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

      {isError && <Alert severity="error">Failed to load this tenant. Please try again.</Alert>}

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
              <Tab label="History" value="history" />
              <Tab label="Settings" value="settings" />
              <Tab label="Subscription" value="subscription" />
            </Tabs>

            {activeTab === 'overview' && (
              <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                <Stack direction="row" sx={{ justifyContent: 'flex-end', mb: 2 }}>
                  <Button variant="contained" onClick={() => setIsEditing(true)}>
                    Edit
                  </Button>
                </Stack>
                <Grid container spacing={3}>
                  <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <DetailField label="Tenant ID" value={tenant.id} />
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
                <TenantUsersTable tenantId={tenant.id} />
              </Paper>
            )}

            {activeTab === 'history' && (
              <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                <TenantHistoryTable tenantId={tenant.id} />
              </Paper>
            )}

            {activeTab === 'subscription' && (
              <SubscriptionTabContent
                currentPlan={currentSub ?? null}
                isLoading={subLoading}
                onChangePlan={() => setIsAssigningPlan(true)}
                subHistory={subHistory}
                historyLoading={historyLoading}
                entitlements={entitlements}
                entitlementsLoading={entitlementsLoading}
                planDetails={planDetails}
                catalogEntitlements={catalogEntitlements}
                tenantId={tenant.id}
              />
            )}

            {activeTab === 'settings' && (
              <Stack spacing={2}>
                <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                  <TenantSettingsPanel tenantId={tenant.id} />
                </Paper>

                <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2 }}>
                  <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
                    <Box>
                      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                        Tenant status
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {tenant.isActive
                          ? 'This tenant is active. Deactivating it will prevent its users from accessing the platform.'
                          : 'This tenant is inactive. Reactivate it to restore access for its users.'}
                      </Typography>
                    </Box>
                    <Button
                      variant="outlined"
                      color={tenant.isActive ? 'error' : 'success'}
                      disabled={setTenantActive.isPending}
                      onClick={() =>
                        setTenantActive.mutate({ id: tenant.id, isActive: !tenant.isActive })
                      }
                    >
                      {tenant.isActive ? 'Deactivate' : 'Activate'}
                    </Button>
                  </Stack>
                </Paper>
              </Stack>
            )}
          </CardContent>
        </Card>
      )}

      <EditTenantDialog
        tenant={tenant ?? null}
        open={isEditing}
        saving={updateTenant.isPending}
        onClose={() => setIsEditing(false)}
        onSave={(values) => {
          if (!tenant) return;
          updateTenant.mutate(
            { id: tenant.id, payload: values },
            { onSuccess: () => setIsEditing(false) },
          );
        }}
      />

      <AssignPlanDialog
        open={isAssigningPlan}
        saving={assignPlan.isPending}
        currentPlanId={currentSub?.planId}
        errorMessage={getApiErrorMessage(assignPlan.error, 'Failed to assign plan.')}
        onClose={() => setIsAssigningPlan(false)}
        onAssign={(payload) => {
          assignPlan.mutate(payload, {
            onSuccess: () => {
              setIsAssigningPlan(false);
              enqueueSnackbar('Plan assigned.', { variant: 'success' });
            },
          });
        }}
      />
    </DashboardLayout>
  );
}
