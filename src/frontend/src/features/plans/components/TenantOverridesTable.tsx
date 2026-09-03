import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import IconButton from '@mui/material/IconButton';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import RestoreOutlinedIcon from '@mui/icons-material/RestoreOutlined';
import type { Entitlement, EntitlementType, PlanEntitlement, ResolvedEntitlements } from '../types/plan';
import { useRemoveOverride, useUpsertOverride } from '../hooks/useSubscriptionMutations';
import { getApiErrorMessage } from '../../../utils/apiError';
import { enqueueSnackbar } from 'notistack';

interface TenantOverridesTableProps {
  tenantId: string;
  entitlements: ResolvedEntitlements;
  planEntitlements: PlanEntitlement[];
  catalogEntitlements: Entitlement[];
  isLoading: boolean;
}

export function TenantOverridesTable({
  tenantId,
  entitlements,
  planEntitlements,
  catalogEntitlements,
  isLoading,
}: TenantOverridesTableProps) {
  const upsert = useUpsertOverride(tenantId);
  const remove = useRemoveOverride(tenantId);

  const [editing, setEditing] = useState<{ key: string; value: string; type: EntitlementType } | null>(null);
  const [editValue, setEditValue] = useState('');
  const [editReason, setEditReason] = useState('');

  const openEdit = (key: string, value: string, type: EntitlementType) => {
    setEditing({ key, value, type });
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

  const handleReset = (key: string) => {
    remove.mutate(key, {
      onSuccess: () => enqueueSnackbar('Reset to plan default.', { variant: 'success' }),
      onError: () => enqueueSnackbar('Failed to reset override.', { variant: 'error' }),
    });
  };

  if (isLoading) {
    return (
      <Stack spacing={1}>
        {Array.from({ length: 3 }, (_, i) => (
          <Skeleton key={i} variant="rectangular" height={52} sx={{ borderRadius: 1 }} />
        ))}
      </Stack>
    );
  }

  const entries = Object.entries(entitlements);

  if (entries.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No entitlements configured for this tenant's plan.
      </Typography>
    );
  }

  return (
    <>
      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Entitlement</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Effective value</TableCell>
              <TableCell>Source</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {entries.map(([key, resolvedValue]) => {
              const catalog = catalogEntitlements.find((e) => e.key === key);
              const planEntry = planEntitlements.find((e) => e.key === key);
              const type: EntitlementType = catalog?.type ?? planEntry?.type ?? 'Integer';
              const isOverride = planEntry !== undefined && planEntry.value !== resolvedValue;

              return (
                <TableRow key={key} hover>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 500, fontFamily: 'monospace' }}>
                      {key}
                    </Typography>
                    {catalog?.description && (
                      <Typography variant="caption" color="text.secondary" display="block">
                        {catalog.description}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip label={type} size="small" variant="outlined" />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: isOverride ? 600 : 400 }}>
                      {resolvedValue}
                    </Typography>
                    {isOverride && planEntry && (
                      <Typography variant="caption" color="text.secondary" display="block">
                        Plan default: {planEntry.value}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    {isOverride ? (
                      <Chip label="Override" color="warning" size="small" />
                    ) : (
                      <Chip label="Plan default" size="small" variant="outlined" />
                    )}
                  </TableCell>
                  <TableCell align="right">
                    <Stack direction="row" justifyContent="flex-end" spacing={0.5}>
                      {isOverride && (
                        <Tooltip title="Reset to plan default">
                          <span>
                            <IconButton
                              size="small"
                              disabled={remove.isPending}
                              onClick={() => handleReset(key)}
                            >
                              <RestoreOutlinedIcon fontSize="small" />
                            </IconButton>
                          </span>
                        </Tooltip>
                      )}
                      <Tooltip title="Edit override">
                        <IconButton size="small" onClick={() => openEdit(key, resolvedValue, type)}>
                          <EditOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </Stack>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={!!editing} onClose={() => setEditing(null)} fullWidth maxWidth="xs">
        <DialogTitle>
          <Stack direction="row" spacing={1} alignItems="baseline">
            <span>Edit override</span>
            <Box component="span" sx={{ fontFamily: 'monospace', fontSize: '0.9em', color: 'text.secondary' }}>
              {editing?.key}
            </Box>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {upsert.error && (
              <Alert severity="error">
                {getApiErrorMessage(upsert.error, 'Failed to save override.')}
              </Alert>
            )}
            {editing?.type === 'Boolean' ? (
              <FormControl fullWidth>
                <InputLabel>Value</InputLabel>
                <Select
                  value={editValue}
                  label="Value"
                  autoFocus
                  onChange={(e) => setEditValue(e.target.value)}
                >
                  <MenuItem value="true">true</MenuItem>
                  <MenuItem value="false">false</MenuItem>
                </Select>
              </FormControl>
            ) : (
              <TextField
                label="Value"
                value={editValue}
                onChange={(e) => setEditValue(e.target.value)}
                type={editing?.type === 'Integer' || editing?.type === 'Decimal' ? 'number' : 'text'}
                autoFocus
                fullWidth
              />
            )}
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
