import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import type { AddTenantUserPayload } from '../api/tenantsApi';

interface AddTenantUserDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  onClose: () => void;
  onAdd: (values: AddTenantUserPayload) => void;
}

const emptyForm: AddTenantUserPayload = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
};

export function AddTenantUserDialog({ open, saving, errorMessage, onClose, onAdd }: AddTenantUserDialogProps) {
  const [form, setForm] = useState<AddTenantUserPayload>(emptyForm);

  const handleClose = () => {
    setForm(emptyForm);
    onClose();
  };

  const isFirstNameValid = form.firstName.trim().length >= 1;
  const isLastNameValid = form.lastName.trim().length >= 1;
  const isEmailValid = /\S+@\S+\.\S+/.test(form.email);
  const isPasswordValid = form.password.length >= 8;

  const isFormValid = isFirstNameValid && isLastNameValid && isEmailValid && isPasswordValid;

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
      <DialogTitle>Add user</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

          <Stack direction="row" spacing={2}>
            <TextField
              label="First name"
              value={form.firstName}
              onChange={(event) => setForm((prev) => ({ ...prev, firstName: event.target.value }))}
              error={form.firstName.length > 0 && !isFirstNameValid}
              autoFocus
              fullWidth
            />
            <TextField
              label="Last name"
              value={form.lastName}
              onChange={(event) => setForm((prev) => ({ ...prev, lastName: event.target.value }))}
              error={form.lastName.length > 0 && !isLastNameValid}
              fullWidth
            />
          </Stack>

          <TextField
            label="Email"
            type="email"
            value={form.email}
            onChange={(event) => setForm((prev) => ({ ...prev, email: event.target.value }))}
            error={form.email.length > 0 && !isEmailValid}
            helperText={form.email.length > 0 && !isEmailValid ? 'Enter a valid email address.' : ' '}
            fullWidth
          />

          <TextField
            label="Password"
            type="password"
            value={form.password}
            onChange={(event) => setForm((prev) => ({ ...prev, password: event.target.value }))}
            error={form.password.length > 0 && !isPasswordValid}
            helperText={
              form.password.length > 0 && !isPasswordValid ? 'Password must be at least 8 characters.' : ' '
            }
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
