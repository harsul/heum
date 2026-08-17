import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import type { AddTenantUserPayload } from '../api/tenantsApi';

interface AddTenantUserDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  onClose: () => void;
  onAdd: (values: AddTenantUserPayload) => void;
}

const emptyForm: AddTenantUserPayload = {
  email: '',
};

export function AddTenantUserDialog({ open, saving, errorMessage, onClose, onAdd }: AddTenantUserDialogProps) {
  const [form, setForm] = useState<AddTenantUserPayload>(emptyForm);

  const handleClose = () => {
    setForm(emptyForm);
    onClose();
  };

  const isEmailValid = /\S+@\S+\.\S+/.test(form.email);
  const isFormValid = isEmailValid;

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
      <DialogTitle>Add user</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

          <Typography variant="body2" color="text.secondary">
            We&apos;ll email this address a link to finish setting up their account (name,
            password, and email verification).
          </Typography>

          <TextField
            label="Email"
            type="email"
            value={form.email}
            onChange={(event) => setForm((prev) => ({ ...prev, email: event.target.value }))}
            error={form.email.length > 0 && !isEmailValid}
            helperText={form.email.length > 0 && !isEmailValid ? 'Enter a valid email address.' : ' '}
            autoFocus
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving}>
          Cancel
        </Button>
        <Button variant="contained" disabled={!isFormValid || saving} onClick={() => onAdd(form)}>
          Add user
        </Button>
      </DialogActions>
    </Dialog>
  );
}
