import { useEffect, useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { getApiErrorMessage } from '../../../utils/apiError';
import { useTenantSettings } from '../hooks/useTenantSettings';
import { useUpdateTenantSettings } from '../hooks/useUpdateTenantSettings';

interface TenantSettingsPanelProps {
  tenantId: string;
}

export function TenantSettingsPanel({ tenantId }: TenantSettingsPanelProps) {
  const { data: settings, isLoading, isError } = useTenantSettings(tenantId);
  const updateSettings = useUpdateTenantSettings(tenantId);
  const [locale, setLocale] = useState('');
  const [timezone, setTimezone] = useState('');

  useEffect(() => {
    if (settings) {
      setLocale(settings.locale);
      setTimezone(settings.timezone);
    }
  }, [settings]);

  const isLocaleValid = locale.trim().length > 0 && locale.trim().length <= 10;
  const isTimezoneValid = timezone.trim().length > 0 && timezone.trim().length <= 100;
  const isFormValid = isLocaleValid && isTimezoneValid;

  function handleLocaleChange(value: string) {
    setLocale(value);
    if (updateSettings.isSuccess || updateSettings.isError) updateSettings.reset();
  }

  function handleTimezoneChange(value: string) {
    setTimezone(value);
    if (updateSettings.isSuccess || updateSettings.isError) updateSettings.reset();
  }

  return (
    <Box>
      <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1 }}>
        Locale &amp; Timezone
      </Typography>
      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress size={24} />
        </Box>
      )}
      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load settings. Please try again.
        </Alert>
      )}
      {settings && (
        <Stack spacing={2} sx={{ maxWidth: 400 }}>
          <TextField
            label="Locale"
            value={locale}
            onChange={(e) => handleLocaleChange(e.target.value)}
            error={locale.length > 0 && !isLocaleValid}
            helperText={locale.length > 0 && !isLocaleValid ? 'Max 10 characters (e.g. en-US).' : ' '}
            fullWidth
          />
          <TextField
            label="Timezone"
            value={timezone}
            onChange={(e) => handleTimezoneChange(e.target.value)}
            error={timezone.length > 0 && !isTimezoneValid}
            helperText={
              timezone.length > 0 && !isTimezoneValid
                ? 'Max 100 characters (e.g. America/New_York).'
                : ' '
            }
            fullWidth
          />
          {updateSettings.isError && (
            <Alert severity="error">
              {getApiErrorMessage(updateSettings.error, 'Failed to save settings.')}
            </Alert>
          )}
          {updateSettings.isSuccess && <Alert severity="success">Settings saved.</Alert>}
          <Box>
            <Button
              variant="contained"
              disabled={!isFormValid || updateSettings.isPending}
              onClick={() =>
                updateSettings.mutate({ locale: locale.trim(), timezone: timezone.trim() })
              }
            >
              Save
            </Button>
          </Box>
        </Stack>
      )}
    </Box>
  );
}
