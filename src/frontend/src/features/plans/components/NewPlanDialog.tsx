import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';

interface NewPlanDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  onClose: () => void;
  onCreate: (name: string) => void;
}

export function NewPlanDialog({ open, saving, errorMessage, onClose, onCreate }: NewPlanDialogProps) {
  const [name, setName] = useState('');

  const handleClose = () => {
    setName('');
    onClose();
  };

  const isValid = name.trim().length >= 2;

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
      <DialogTitle>New plan</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
          <TextField
            label="Plan name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            error={name.length > 0 && !isValid}
            helperText={name.length > 0 && !isValid ? 'Name must be at least 2 characters.' : ' '}
            autoFocus
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving} color="inherit">
          Cancel
        </Button>
        <Button variant="contained" disabled={!isValid || saving} onClick={() => onCreate(name.trim())}>
          Create plan
        </Button>
      </DialogActions>
    </Dialog>
  );
}
