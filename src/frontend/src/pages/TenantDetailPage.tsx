import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import CircularProgress from '@mui/material/CircularProgress';
import Divider from '@mui/material/Divider';
import Grid from '@mui/material/Grid';
import IconButton from '@mui/material/IconButton';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import BlockOutlinedIcon from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { EditTenantDialog } from '../features/tenants/components/EditTenantDialog';
import { useTenant } from '../features/tenants/hooks/useTenant';
import { useSetTenantActive, useUpdateTenant } from '../features/tenants/hooks/useTenantMutations';
import { formatDate, tenantInitials } from '../features/tenants/utils';

function DetailField({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value}</Typography>
    </Box>
  );
}

export function TenantDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: tenant, isLoading, isError } = useTenant(id);
  const updateTenant = useUpdateTenant();
  const setTenantActive = useSetTenantActive();
  const [isEditing, setIsEditing] = useState(false);

  return (
    <DashboardLayout>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 4 }}>
        <IconButton onClick={() => navigate('/tenants')} aria-label="Back to tenants">
          <ArrowBackOutlinedIcon />
        </IconButton>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>
            Tenant overview
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Details for a single tenant on your platform.
          </Typography>
        </Box>
      </Stack>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
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

              <Stack direction="row" spacing={1}>
                <Button variant="outlined" startIcon={<EditOutlinedIcon />} onClick={() => setIsEditing(true)}>
                  Edit
                </Button>
                <Button
                  variant="outlined"
                  color={tenant.isActive ? 'error' : 'success'}
                  startIcon={tenant.isActive ? <BlockOutlinedIcon /> : <CheckCircleOutlineIcon />}
                  disabled={setTenantActive.isPending}
                  onClick={() =>
                    setTenantActive.mutate({ id: tenant.id, isActive: !tenant.isActive })
                  }
                >
                  {tenant.isActive ? 'Deactivate' : 'Reactivate'}
                </Button>
              </Stack>
            </Stack>

            <Divider sx={{ mb: 3 }} />

            <Grid container spacing={3}>
              <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                <DetailField label="Tenant ID" value={tenant.id} />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                <DetailField
                  label="Status"
                  value={
                    <Chip
                      size="small"
                      label={tenant.isActive ? 'Active' : 'Inactive'}
                      color={tenant.isActive ? 'success' : 'default'}
                      variant={tenant.isActive ? 'filled' : 'outlined'}
                    />
                  }
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                <DetailField label="Created" value={formatDate(tenant.createdAtUtc)} />
              </Grid>
              <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                <DetailField label="Last updated" value={formatDate(tenant.updatedAtUtc)} />
              </Grid>
            </Grid>
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
    </DashboardLayout>
  );
}
