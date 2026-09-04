import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import MuiLink from '@mui/material/Link';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { PlanEntitlementsTable } from '../features/plans/components/PlanEntitlementsTable';
import { useEntitlements, usePlan } from '../features/plans/hooks/usePlans';
import { useUpdatePlan } from '../features/plans/hooks/usePlanMutations';
import { getApiErrorMessage } from '../utils/apiError';
import { enqueueSnackbar } from 'notistack';

export function PlanDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: plan, isLoading, isError } = usePlan(id);
  const { data: entitlements = [], isLoading: entitlementsLoading } = useEntitlements();
  const updatePlan = useUpdatePlan();

  const [editName, setEditName] = useState('');
  const [editActive, setEditActive] = useState(true);
  const [isDirty, setIsDirty] = useState(false);

  useEffect(() => {
    if (plan) {
      setEditName(plan.name);
      setEditActive(plan.isActive);
      setIsDirty(false);
    }
  }, [plan]);

  const handleSave = () => {
    if (!plan) return;
    updatePlan.mutate(
      { id: plan.id, payload: { name: editName.trim(), isActive: editActive } },
      {
        onSuccess: () => {
          setIsDirty(false);
          enqueueSnackbar('Plan updated.', { variant: 'success' });
        },
        onError: (err) =>
          enqueueSnackbar(getApiErrorMessage(err, 'Failed to update plan.'), { variant: 'error' }),
      },
    );
  };

  return (
    <DashboardLayout>
      <Breadcrumbs sx={{ mb: 2 }}>
        <MuiLink component={Link} to="/admin/plans" underline="hover" color="inherit">
          Plans
        </MuiLink>
        <Typography color="text.primary">{plan?.name ?? '…'}</Typography>
      </Breadcrumbs>

      {isError && <Alert severity="error">Failed to load plan. Please try again.</Alert>}

      {isLoading && (
        <Card>
          <CardContent sx={{ p: 4 }}>
            <Skeleton variant="text" width={200} sx={{ fontSize: '1.5rem', mb: 2 }} />
            <Skeleton variant="rectangular" height={56} sx={{ mb: 2, borderRadius: 1 }} />
            <Skeleton variant="rectangular" height={200} sx={{ borderRadius: 1 }} />
          </CardContent>
        </Card>
      )}

      {plan && (
        <Card>
          <CardContent sx={{ p: 4 }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between', mb: 3 }}
              spacing={2}
            >
              <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                <Typography variant="h5" sx={{ fontWeight: 700 }}>
                  {plan.name}
                </Typography>
                <Chip
                  size="small"
                  label={plan.isActive ? 'Active' : 'Inactive'}
                  color={plan.isActive ? 'success' : 'warning'}
                />
              </Stack>
              <Button
                variant="contained"
                disabled={!isDirty || updatePlan.isPending || editName.trim().length < 2}
                onClick={handleSave}
              >
                Save changes
              </Button>
            </Stack>

            <Stack spacing={2} sx={{ mb: 4, maxWidth: 400 }}>
              <TextField
                label="Plan name"
                value={editName}
                onChange={(e) => {
                  setEditName(e.target.value);
                  setIsDirty(true);
                }}
                error={editName.trim().length > 0 && editName.trim().length < 2}
                helperText={
                  editName.trim().length > 0 && editName.trim().length < 2
                    ? 'Name must be at least 2 characters.'
                    : ' '
                }
                fullWidth
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={editActive}
                    onChange={(e) => {
                      setEditActive(e.target.checked);
                      setIsDirty(true);
                    }}
                  />
                }
                label={editActive ? 'Active' : 'Inactive'}
              />
            </Stack>

            <Divider sx={{ mb: 3 }} />

            <Box>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
                Entitlements
              </Typography>
              <PlanEntitlementsTable
                plan={plan}
                entitlements={entitlements}
                entitlementsLoading={entitlementsLoading}
              />
            </Box>
          </CardContent>
        </Card>
      )}
    </DashboardLayout>
  );
}
