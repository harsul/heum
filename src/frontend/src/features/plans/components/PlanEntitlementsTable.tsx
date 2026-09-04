import { useState } from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import FormControl from '@mui/material/FormControl';
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
import Typography from '@mui/material/Typography';
import type { Entitlement, Plan } from '../types/plan';
import { useUpsertPlanEntitlement } from '../hooks/usePlanMutations';
import { enqueueSnackbar } from 'notistack';

interface PlanEntitlementsTableProps {
  plan: Plan;
  entitlements: Entitlement[];
  entitlementsLoading: boolean;
}

export function PlanEntitlementsTable({
  plan,
  entitlements,
  entitlementsLoading,
}: PlanEntitlementsTableProps) {
  const upsert = useUpsertPlanEntitlement();
  const [draftValues, setDraftValues] = useState<Record<string, string>>({});

  const currentValue = (key: string): string => {
    const pe = plan.entitlements.find((e) => e.key === key);
    return pe?.value ?? '';
  };

  const draftOrCurrent = (key: string) => draftValues[key] ?? currentValue(key);

  const handleSave = (entitlement: Entitlement) => {
    const value = draftOrCurrent(entitlement.key);
    upsert.mutate(
      { planId: plan.id, key: entitlement.key, value },
      {
        onSuccess: () => {
          setDraftValues((prev) => {
            const next = { ...prev };
            delete next[entitlement.key];
            return next;
          });
          enqueueSnackbar('Entitlement saved.', { variant: 'success' });
        },
        onError: () => enqueueSnackbar('Failed to save entitlement.', { variant: 'error' }),
      },
    );
  };

  if (entitlementsLoading) {
    return (
      <Stack spacing={1}>
        {Array.from({ length: 3 }, (_, i) => (
          <Skeleton key={i} variant="rectangular" height={52} sx={{ borderRadius: 1 }} />
        ))}
      </Stack>
    );
  }

  if (entitlements.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No entitlements defined yet.
      </Typography>
    );
  }

  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Key</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Value</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {entitlements.map((entitlement) => {
            const draft = draftOrCurrent(entitlement.key);
            const original = currentValue(entitlement.key);
            const isDirty = draftValues[entitlement.key] !== undefined && draftValues[entitlement.key] !== original;

            return (
              <TableRow key={entitlement.key}>
                <TableCell>
                  <Box>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {entitlement.key}
                    </Typography>
                    {entitlement.description && (
                      <Typography variant="caption" color="text.secondary">
                        {entitlement.description}
                      </Typography>
                    )}
                  </Box>
                </TableCell>
                <TableCell>
                  <Chip label={entitlement.type} size="small" variant="outlined" />
                </TableCell>
                <TableCell sx={{ minWidth: 160 }}>
                  {entitlement.type === 'Boolean' ? (
                    <FormControl size="small" sx={{ minWidth: 100 }}>
                      <Select
                        value={draft || 'false'}
                        onChange={(e) =>
                          setDraftValues((prev) => ({ ...prev, [entitlement.key]: e.target.value }))
                        }
                      >
                        <MenuItem value="true">true</MenuItem>
                        <MenuItem value="false">false</MenuItem>
                      </Select>
                    </FormControl>
                  ) : (
                    <TextField
                      size="small"
                      value={draft}
                      placeholder="Not set"
                      onChange={(e) =>
                        setDraftValues((prev) => ({ ...prev, [entitlement.key]: e.target.value }))
                      }
                      sx={{ width: 120 }}
                    />
                  )}
                </TableCell>
                <TableCell>
                  <Button
                    size="small"
                    variant={isDirty ? 'contained' : 'outlined'}
                    disabled={upsert.isPending || !draft}
                    onClick={() => handleSave(entitlement)}
                  >
                    Save
                  </Button>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
