import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControlLabel from '@mui/material/FormControlLabel';
import IconButton from '@mui/material/IconButton';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import AddIcon from '@mui/icons-material/Add';
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import FlagOutlinedIcon from '@mui/icons-material/FlagOutlined';
import { enqueueSnackbar } from 'notistack';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import {
  createFeatureFlag,
  deleteFeatureFlag,
  fetchAdminFeatureFlags,
  updateFeatureFlag,
} from '../features/features/api/featuresApi';
import type {
  CreateFeatureFlagRequest,
  FeatureFlagResponse,
  UpdateFeatureFlagRequest,
} from '../features/features/api/featuresApi';
import { getApiErrorMessage } from '../utils/apiError';

// ── Status helpers ───────────────────────────────────────────────────────────

function flagStatus(flag: FeatureFlagResponse): { label: string; color: 'default' | 'success' | 'info' | 'warning' } {
  if (!flag.isEnabled) return { label: 'Disabled', color: 'default' };
  if (flag.defaultRolloutPercentage === 100) return { label: 'All users', color: 'success' };
  if (flag.targetedTenantIds.length > 0) {
    return {
      label: `${flag.targetedTenantIds.length} tenant${flag.targetedTenantIds.length > 1 ? 's' : ''}`,
      color: 'info',
    };
  }
  return { label: 'Enabled', color: 'success' };
}

// ── New flag dialog ──────────────────────────────────────────────────────────

interface NewFlagDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string | null;
  onClose: () => void;
  onCreate: (req: CreateFeatureFlagRequest) => void;
}

function NewFlagDialog({ open, saving, errorMessage, onClose, onCreate }: NewFlagDialogProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  const handleCreate = () => {
    if (!name.trim()) return;
    onCreate({ name: name.trim(), description: description.trim() || undefined });
  };

  const handleClose = () => {
    setName('');
    setDescription('');
    onClose();
  };

  const nameError = name.trim().length > 0 && !/^[A-Za-z][A-Za-z0-9_]*$/.test(name.trim());

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="xs" fullWidth>
      <DialogTitle>New feature flag</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
          <TextField
            label="Key (PascalCase)"
            placeholder="CustomDomains"
            value={name}
            onChange={(e) => setName(e.target.value)}
            error={nameError}
            helperText={
              nameError
                ? 'Use letters, digits and underscores. Must start with a letter.'
                : 'This is the identifier you use in code: useFeatureFlag("CustomDomains")'
            }
            fullWidth
            autoFocus
          />
          <TextField
            label="Description (optional)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            fullWidth
            multiline
            rows={2}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button
          variant="contained"
          disabled={!name.trim() || nameError || saving}
          onClick={handleCreate}
        >
          {saving ? 'Creating…' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ── Configure targeting dialog ───────────────────────────────────────────────

interface ConfigureDialogProps {
  flag: FeatureFlagResponse | null;
  saving: boolean;
  errorMessage?: string | null;
  onClose: () => void;
  onSave: (req: UpdateFeatureFlagRequest) => void;
}

function ConfigureDialog({ flag, saving, errorMessage, onClose, onSave }: ConfigureDialogProps) {
  const [isEnabled, setIsEnabled] = useState(false);
  const [globally, setGlobally] = useState(false);
  const [tenants, setTenants] = useState<string[]>([]);
  const [tenantInput, setTenantInput] = useState('');
  const [users, setUsers] = useState<string[]>([]);
  const [userInput, setUserInput] = useState('');

  // Sync local form state whenever a different flag is selected.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    if (flag) {
      setIsEnabled(flag.isEnabled);
      setGlobally(flag.defaultRolloutPercentage === 100);
      setTenants([...flag.targetedTenantIds]);
      setUsers([...flag.targetedUserIds]);
      setTenantInput('');
      setUserInput('');
    }
  }, [flag?.name]);

  const addTenant = () => {
    const id = tenantInput.trim();
    if (id && !tenants.includes(id)) setTenants((prev) => [...prev, id]);
    setTenantInput('');
  };

  const addUser = () => {
    const id = userInput.trim();
    if (id && !users.includes(id)) setUsers((prev) => [...prev, id]);
    setUserInput('');
  };

  const handleSave = () => {
    if (!flag) return;
    onSave({
      isEnabled,
      description: flag.description,
      targetedTenantIds: globally ? [] : tenants,
      targetedUserIds: globally ? [] : users,
      defaultRolloutPercentage: globally ? 100 : 0,
    });
  };

  return (
    <Dialog open={flag !== null} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Configure — {flag?.name}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

          <FormControlLabel
            control={<Switch checked={isEnabled} onChange={(e) => setIsEnabled(e.target.checked)} />}
            label={isEnabled ? 'Enabled' : 'Disabled'}
          />

          {isEnabled && (
            <FormControlLabel
              control={
                <Switch
                  checked={globally}
                  onChange={(e) => {
                    setGlobally(e.target.checked);
                    if (e.target.checked) { setTenants([]); setUsers([]); }
                  }}
                />
              }
              label="Enable for all users (100% rollout)"
            />
          )}

          {isEnabled && !globally && (
            <>
              <Box>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  Targeted tenants
                </Typography>
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mb: 1.5 }}>
                  {tenants.map((id) => (
                    <Chip key={id} label={id} size="small" onDelete={() => setTenants((prev) => prev.filter((t) => t !== id))} />
                  ))}
                  {tenants.length === 0 && (
                    <Typography variant="body2" color="text.secondary">No tenants targeted yet.</Typography>
                  )}
                </Stack>
                <Stack direction="row" spacing={1}>
                  <TextField
                    size="small"
                    placeholder="Paste tenant ID"
                    value={tenantInput}
                    onChange={(e) => setTenantInput(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addTenant(); } }}
                    sx={{ flex: 1 }}
                  />
                  <Button variant="outlined" size="small" onClick={addTenant}>Add</Button>
                </Stack>
              </Box>

              <Box>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  Targeted users
                </Typography>
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mb: 1.5 }}>
                  {users.map((id) => (
                    <Chip key={id} label={id} size="small" onDelete={() => setUsers((prev) => prev.filter((u) => u !== id))} />
                  ))}
                  {users.length === 0 && (
                    <Typography variant="body2" color="text.secondary">No users targeted yet.</Typography>
                  )}
                </Stack>
                <Stack direction="row" spacing={1}>
                  <TextField
                    size="small"
                    placeholder="Paste user subject (sub)"
                    value={userInput}
                    onChange={(e) => setUserInput(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addUser(); } }}
                    sx={{ flex: 1 }}
                  />
                  <Button variant="outlined" size="small" onClick={addUser}>Add</Button>
                </Stack>
              </Box>
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={saving} onClick={handleSave}>
          {saving ? 'Saving…' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function FeaturesPage() {
  const queryClient = useQueryClient();
  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin', 'features'],
    queryFn: fetchAdminFeatureFlags,
  });

  const [newOpen, setNewOpen] = useState(false);
  const [configuring, setConfiguring] = useState<FeatureFlagResponse | null>(null);
  const [deleting, setDeleting] = useState<FeatureFlagResponse | null>(null);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin', 'features'] });

  const createMutation = useMutation({
    mutationFn: createFeatureFlag,
    onSuccess: () => { invalidate(); setNewOpen(false); enqueueSnackbar('Feature flag created.', { variant: 'success' }); },
  });

  const updateMutation = useMutation({
    mutationFn: ({ name, req }: { name: string; req: UpdateFeatureFlagRequest }) => updateFeatureFlag(name, req),
    onSuccess: () => { invalidate(); setConfiguring(null); enqueueSnackbar('Feature flag updated.', { variant: 'success' }); },
  });

  const deleteMutation = useMutation({
    mutationFn: (name: string) => deleteFeatureFlag(name),
    onSuccess: () => { invalidate(); setDeleting(null); enqueueSnackbar('Feature flag deleted.', { variant: 'success' }); },
  });

  const isManageable = data?.isManageable ?? false;
  const flags = data?.flags ?? [];

  return (
    <DashboardLayout>
      <Card>
        <Stack
          direction="row"
          sx={{ alignItems: 'center', justifyContent: 'space-between', px: 3, pt: 3, pb: 2 }}
        >
          <Box>
            <Typography variant="h6">Feature Flags</Typography>
            <Typography variant="body2" color="text.secondary">
              Control feature rollout per tenant or user via Azure App Configuration.
            </Typography>
          </Box>
          {isManageable && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => { createMutation.reset(); setNewOpen(true); }}
            >
              New flag
            </Button>
          )}
        </Stack>

        {!isManageable && !isLoading && (
          <Alert severity="info" sx={{ mx: 3, mb: 2 }}>
            Azure App Configuration is not connected — flags below are read-only from{' '}
            <strong>appsettings.Development.json</strong>. Set{' '}
            <code>ConnectionStrings:appconfig</code> in user secrets to enable full management.
          </Alert>
        )}

        {isError && (
          <Alert severity="error" sx={{ mx: 3, mb: 2 }}>
            Failed to load feature flags.
          </Alert>
        )}

        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Description</TableCell>
                {isManageable && <TableCell align="right">Actions</TableCell>}
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading &&
                Array.from({ length: 3 }, (_, i) => (
                  <TableRow key={i}>
                    <TableCell><Skeleton variant="text" width={120} /></TableCell>
                    <TableCell><Skeleton variant="rounded" width={72} height={24} /></TableCell>
                    <TableCell><Skeleton variant="text" width={200} /></TableCell>
                    {isManageable && <TableCell />}
                  </TableRow>
                ))}

              {!isLoading && flags.map((flag) => {
                const { label, color } = flagStatus(flag);
                return (
                  <TableRow key={flag.name}>
                    <TableCell>
                      <Typography
                        variant="body2"
                        sx={{ fontFamily: 'monospace', fontWeight: 600, fontSize: '0.8125rem' }}
                      >
                        {flag.name}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={label} size="small" color={color} />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" color="text.secondary">
                        {flag.description ?? '—'}
                      </Typography>
                    </TableCell>
                    {isManageable && (
                      <TableCell align="right">
                        <Tooltip title="Configure targeting">
                          <IconButton
                            size="small"
                            onClick={() => { updateMutation.reset(); setConfiguring(flag); }}
                          >
                            <EditOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Delete">
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => setDeleting(flag)}
                          >
                            <DeleteOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    )}
                  </TableRow>
                );
              })}

              {!isLoading && flags.length === 0 && !isError && (
                <TableRow>
                  <TableCell colSpan={isManageable ? 4 : 3} sx={{ border: 0 }}>
                    <EmptyState
                      icon={FlagOutlinedIcon}
                      title="No feature flags yet"
                      description="Create a flag and assign it to specific tenants or users."
                      action={
                        isManageable ? (
                          <Button
                            variant="outlined"
                            size="small"
                            onClick={() => { createMutation.reset(); setNewOpen(true); }}
                          >
                            New flag
                          </Button>
                        ) : undefined
                      }
                    />
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      <NewFlagDialog
        open={newOpen}
        saving={createMutation.isPending}
        errorMessage={getApiErrorMessage(createMutation.error, 'Failed to create flag.')}
        onClose={() => setNewOpen(false)}
        onCreate={(req) => createMutation.mutate(req)}
      />

      <ConfigureDialog
        flag={configuring}
        saving={updateMutation.isPending}
        errorMessage={getApiErrorMessage(updateMutation.error, 'Failed to update flag.')}
        onClose={() => setConfiguring(null)}
        onSave={(req) => {
          if (!configuring) return;
          updateMutation.mutate({ name: configuring.name, req });
        }}
      />

      <ConfirmDialog
        open={deleting !== null}
        title="Delete feature flag"
        description={`Delete "${deleting?.name}"? This cannot be undone.`}
        confirmLabel="Delete"
        confirmColor="error"
        loading={deleteMutation.isPending}
        onClose={() => setDeleting(null)}
        onConfirm={() => { if (deleting) deleteMutation.mutate(deleting.name); }}
      />
    </DashboardLayout>
  );
}
