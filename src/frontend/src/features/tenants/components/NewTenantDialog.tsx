import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import type { CreateTenantPayload } from '../api/tenantsApi';

interface NewTenantDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  onClose: () => void;
  onCreate: (values: CreateTenantPayload) => void;
}

const SLUG_PATTERN = /^[a-z0-9]+(-[a-z0-9]+)*$/;

function slugify(value: string) {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

const emptyForm: CreateTenantPayload = {
  companyName: '',
  slug: '',
  adminFirstName: '',
  adminLastName: '',
  adminEmail: '',
  adminPassword: '',
};

export function NewTenantDialog({ open, saving, errorMessage, onClose, onCreate }: NewTenantDialogProps) {
  const [form, setForm] = useState<CreateTenantPayload>(emptyForm);
  const [slugTouched, setSlugTouched] = useState(false);

  const handleClose = () => {
    setForm(emptyForm);
    setSlugTouched(false);
    onClose();
  };

  const isCompanyNameValid = form.companyName.trim().length >= 2;
  const isSlugValid = SLUG_PATTERN.test(form.slug);
  const isAdminFirstNameValid = form.adminFirstName.trim().length >= 1;
  const isAdminLastNameValid = form.adminLastName.trim().length >= 1;
  const isAdminEmailValid = /\S+@\S+\.\S+/.test(form.adminEmail);
  const isAdminPasswordValid = form.adminPassword.length >= 8;

  const isFormValid =
    isCompanyNameValid &&
    isSlugValid &&
    isAdminFirstNameValid &&
    isAdminLastNameValid &&
    isAdminEmailValid &&
    isAdminPasswordValid;

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
      <DialogTitle>New tenant</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

          <TextField
            label="Company name"
            value={form.companyName}
            onChange={(event) => {
              const companyName = event.target.value;
              setForm((prev) => ({
                ...prev,
                companyName,
                slug: slugTouched ? prev.slug : slugify(companyName),
              }));
            }}
            error={form.companyName.length > 0 && !isCompanyNameValid}
            helperText={
              form.companyName.length > 0 && !isCompanyNameValid
                ? 'Company name must be at least 2 characters.'
                : ' '
            }
            autoFocus
            fullWidth
          />

          <TextField
            label="Slug"
            value={form.slug}
            onChange={(event) => {
              setSlugTouched(true);
              setForm((prev) => ({ ...prev, slug: event.target.value }));
            }}
            error={form.slug.length > 0 && !isSlugValid}
            helperText={
              form.slug.length > 0 && !isSlugValid
                ? 'Slug must be lowercase alphanumeric words separated by hyphens.'
                : ' '
            }
            fullWidth
          />

          <Stack direction="row" spacing={2}>
            <TextField
              label="Admin first name"
              value={form.adminFirstName}
              onChange={(event) => setForm((prev) => ({ ...prev, adminFirstName: event.target.value }))}
              error={form.adminFirstName.length > 0 && !isAdminFirstNameValid}
              fullWidth
            />
            <TextField
              label="Admin last name"
              value={form.adminLastName}
              onChange={(event) => setForm((prev) => ({ ...prev, adminLastName: event.target.value }))}
              error={form.adminLastName.length > 0 && !isAdminLastNameValid}
              fullWidth
            />
          </Stack>

          <TextField
            label="Admin email"
            type="email"
            value={form.adminEmail}
            onChange={(event) => setForm((prev) => ({ ...prev, adminEmail: event.target.value }))}
            error={form.adminEmail.length > 0 && !isAdminEmailValid}
            helperText={form.adminEmail.length > 0 && !isAdminEmailValid ? 'Enter a valid email address.' : ' '}
            fullWidth
          />

          <TextField
            label="Admin password"
            type="password"
            value={form.adminPassword}
            onChange={(event) => setForm((prev) => ({ ...prev, adminPassword: event.target.value }))}
            error={form.adminPassword.length > 0 && !isAdminPasswordValid}
            helperText={
              form.adminPassword.length > 0 && !isAdminPasswordValid
                ? 'Password must be at least 8 characters.'
                : ' '
            }
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving}>
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
