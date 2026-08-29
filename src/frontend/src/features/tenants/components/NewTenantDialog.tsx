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
import type { CreateTenantPayload } from '../api/tenantsApi';

interface NewTenantDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  onClose: () => void;
  onCreate: (values: CreateTenantPayload) => void;
}

const emptyForm: CreateTenantPayload = {
  companyName: '',
};

export function NewTenantDialog({ open, saving, errorMessage, onClose, onCreate }: NewTenantDialogProps) {
  const [form, setForm] = useState<CreateTenantPayload>(emptyForm);

  const handleClose = () => {
    setForm(emptyForm);
    onClose();
  };

  const isCompanyNameValid = form.companyName.trim().length >= 2;

  const isFormValid = isCompanyNameValid;

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
      <DialogTitle>New tenant</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

          <Typography variant="body2" color="text.secondary">
            A URL slug will be generated automatically. You can add users to the tenant after
            creation.
          </Typography>

          <TextField
            label="Company name"
            value={form.companyName}
            onChange={(event) => setForm((prev) => ({ ...prev, companyName: event.target.value }))}
            error={form.companyName.length > 0 && !isCompanyNameValid}
            helperText={
              form.companyName.length > 0 && !isCompanyNameValid
                ? 'Company name must be at least 2 characters.'
                : ' '
            }
            autoFocus
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving} color="inherit">
          Cancel
        </Button>
        <Button
          variant="contained"
          disabled={!isFormValid || saving}
          onClick={() => onCreate(form)}
        >
          Create tenant
        </Button>
      </DialogActions>
    </Dialog>
  );
}
