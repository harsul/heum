import { useState } from 'react';
import Alert from '@mui/material/Alert';
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
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import type { ResolvedEntitlements } from '../types/plan';
import { useUpsertOverride } from '../hooks/useSubscriptionMutations';
import { getApiErrorMessage } from '../../../utils/apiError';
import { enqueueSnackbar } from 'notistack';

interface TenantOverridesTableProps {
  tenantId: string;
  entitlements: ResolvedEntitlements;
  isLoading: boolean;
}

export function TenantOverridesTable({ tenantId, entitlements, isLoading }: TenantOverridesTableProps) {
  const upsert = useUpsertOverride(tenantId);

  const [editing, setEditing] = useState<{ key: string; value: string } | null>(null);
  const [editValue, setEditValue] = useState('');
  const [editReason, setEditReason] = useState('');

  const openEdit = (key: string, value: string) => {
    setEditing({ key, value });
    setEditValue(value);
    setEditReason('');
  };

  const handleSave = () => {
    if (!editing) return;
    upsert.mutate(
      { key: editing.key, value: editValue, reason: editReason.trim() || undefined },
      {
        onSuccess: () => {
          setEditing(null);
          enqueueSnackbar('Override saved.', { variant: 'success' });
        },
        onError: () => enqueueSnackbar('Failed to save override.', { variant: 'error' }),
      },
    );
  };

  if (isLoading) {
    return (
      <Stack spacing={1}>
        {Array.from({ length: 3 }, (_, i) => (
          <Skeleton key={i} variant="rectangular" height={44} sx={{ borderRadius: 1 }} />
        ))}
      </Stack>
    );
  }

  const entries = Object.entries(entitlements);

  if (entries.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No entitlements configured for this tenant.
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
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {entries.map(([key, value]) => (
              <TableRow key={key}>
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {key}
                  </Typography>
                </TableCell>
                <TableCell>{value}</TableCell>
                <TableCell align="right">
                  <IconButton size="small" onClick={() => openEdit(key, value)}>
                    <EditOutlinedIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={!!editing} onClose={() => setEditing(null)} fullWidth maxWidth="xs">
        <DialogTitle>Override — {editing?.key}</DialogTitle>
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
