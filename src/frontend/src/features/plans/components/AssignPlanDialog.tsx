import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import { usePlans } from '../hooks/usePlans';

interface AssignPlanDialogProps {
  open: boolean;
  saving: boolean;
  currentPlanId?: string;
  errorMessage?: string;
  onClose: () => void;
  onAssign: (payload: { planId: string; notes?: string }) => void;
}

export function AssignPlanDialog({
  open,
  saving,
  currentPlanId,
  errorMessage,
  onClose,
  onAssign,
}: AssignPlanDialogProps) {
  const { data: plans = [], isLoading } = usePlans();
  const [planId, setPlanId] = useState('');
  const [notes, setNotes] = useState('');

  const handleClose = () => {
    setPlanId('');
    setNotes('');
    onClose();
  };

  const activePlans = plans.filter((p) => p.isActive);

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
      <DialogTitle>Change plan</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
          <FormControl fullWidth disabled={isLoading}>
            <InputLabel>Plan</InputLabel>
            {isLoading ? (
              <CircularProgress size={20} sx={{ m: 'auto' }} />
            ) : (
              <Select value={planId} label="Plan" onChange={(e) => setPlanId(e.target.value)}>
                {activePlans.map((p) => (
                  <MenuItem key={p.id} value={p.id} disabled={p.id === currentPlanId}>
                    {p.name} {p.id === currentPlanId ? '(current)' : ''}
                  </MenuItem>
                ))}
              </Select>
            )}
          </FormControl>
          <TextField
            label="Notes (optional)"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            fullWidth
            multiline
            rows={2}
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving} color="inherit">
          Cancel
        </Button>
        <Button
          variant="contained"
          disabled={!planId || saving}
          onClick={() => onAssign({ planId, notes: notes.trim() || undefined })}
        >
          Assign plan
        </Button>
      </DialogActions>
    </Dialog>
  );
}
