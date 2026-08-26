import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

interface AddUserByEmailDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  roles?: string[];
  rolesLoading?: boolean;
  onClose: () => void;
  onAdd: (values: { email: string; role?: string }) => void;
}

const emptyForm = { email: '', role: '' };

export function AddUserByEmailDialog({
  open,
  saving,
  errorMessage,
  roles,
  rolesLoading,
  onClose,
  onAdd,
}: AddUserByEmailDialogProps) {
  const [form, setForm] = useState(emptyForm);

  const handleClose = () => {
    setForm(emptyForm);
    onClose();
  };

  const isEmailValid = /\S+@\S+\.\S+/.test(form.email);
  const showRoleSelect = rolesLoading || (roles && roles.length > 0);

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

          {showRoleSelect && (
            rolesLoading ? (
              <Skeleton variant="rounded" height={56} />
            ) : (
              <FormControl fullWidth>
                <InputLabel id="add-user-role-label">Role</InputLabel>
                <Select
                  labelId="add-user-role-label"
                  label="Role"
                  value={form.role}
                  onChange={(event) => setForm((prev) => ({ ...prev, role: event.target.value }))}
                >
                  <MenuItem value="">
                    <em>Standard user</em>
                  </MenuItem>
                  {roles!.map((role) => (
                    <MenuItem key={role} value={role}>
                      {role}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )
          )}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving}>
          Cancel
        </Button>
        <Button
          variant="contained"
          disabled={!isEmailValid || saving}
          onClick={() => onAdd({ email: form.email, role: form.role || undefined })}
        >
          Add user
        </Button>
      </DialogActions>
    </Dialog>
  );
}
