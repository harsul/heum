import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import IconButton from '@mui/material/IconButton';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import type { TenantEntitlementOverride } from '../types/plan';
import { useRemoveOverride, useUpsertOverride } from '../hooks/useSubscriptionMutations';
import { getApiErrorMessage } from '../../../utils/apiError';
import { enqueueSnackbar } from 'notistack';

interface TenantOverridesTableProps {
  tenantId: string;
  overrides: TenantEntitlementOverride[];
  isLoading: boolean;
}

export function TenantOverridesTable({ tenantId, overrides, isLoading }: TenantOverridesTableProps) {
  const upsert = useUpsertOverride(tenantId);
  const remove = useRemoveOverride(tenantId);

  const [editing, setEditing] = useState<TenantEntitlementOverride | null>(null);
  const [editValue, setEditValue] = useState('');
  const [editReason, setEditReason] = useState('');

  const openEdit = (override: TenantEntitlementOverride) => {
    setEditing(override);
    setEditValue(override.value);
    setEditReason(override.reason ?? '');
  };

  const handleSave = () => {
    if (!editing) return;
    upsert.mutate(
      { key: editing.entitlementKey, value: editValue, reason: editReason.trim() || undefined },
      {
        onSuccess: () => {
          setEditing(null);
          enqueueSnackbar('Override saved.', { variant: 'success' });
        },
        onError: () => enqueueSnackbar('Failed to save override.', { variant: 'error' }),
      },
    );
  };

  const handleDelete = (key: string) => {
    remove.mutate(key, {
      onSuccess: () => enqueueSnackbar('Override removed.', { variant: 'success' }),
      onError: () => enqueueSnackbar('Failed to remove override.', { variant: 'error' }),
    });
  };

  if (isLoading) {
    return (
      <Stack spacing={1}>
        {Array.from({ length: 2 }, (_, i) => (
          <Skeleton key={i} variant="rectangular" height={44} sx={{ borderRadius: 1 }} />
        ))}
      </Stack>
    );
  }

  if (overrides.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No overrides for this tenant.
      </Typography>
    );
  }

  return (
    <>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Key</TableCell>
              <TableCell>Value</TableCell>
              <TableCell>Reason</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {overrides.map((o) => (
              <TableRow key={o.entitlementKey}>
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {o.entitlementKey}
                  </Typography>
                </TableCell>
                <TableCell>{o.value}</TableCell>
                <TableCell>{o.reason ?? '—'}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5}>
                    <IconButton size="small" onClick={() => openEdit(o)}>
                      <EditOutlinedIcon fontSize="small" />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="error"
                      disabled={remove.isPending}
                      onClick={() => handleDelete(o.entitlementKey)}
                    >
                      <DeleteOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={!!editing} onClose={() => setEditing(null)} fullWidth maxWidth="xs">
        <DialogTitle>Edit override — {editing?.entitlementKey}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {upsert.error && (
              <Alert severity="error">
                {getApiErrorMessage(upsert.error, 'Failed to save override.')}
              </Alert>
            )}
            <TextField
              label="Value"
              value={editValue}
              onChange={(e) => setEditValue(e.target.value)}
              autoFocus
              fullWidth
            />
            <TextField
              label="Reason (optional)"
              value={editReason}
              onChange={(e) => setEditReason(e.target.value)}
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={() => setEditing(null)} disabled={upsert.isPending} color="inherit">
            Cancel
          </Button>
          <Button
            variant="contained"
            disabled={!editValue || upsert.isPending}
            onClick={handleSave}
          >
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

export function TenantSubscriptionPanel({ tenantId }: { tenantId: string }) {
  return <Box>{tenantId}</Box>;
}
