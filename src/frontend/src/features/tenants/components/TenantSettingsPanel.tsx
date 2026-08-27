import { useEffect, useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Skeleton from '@mui/material/Skeleton';
import FormControl from '@mui/material/FormControl';
import Grid from '@mui/material/Grid';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { getApiErrorMessage } from '../../../utils/apiError';
import { useTenantSettings } from '../hooks/useTenantSettings';
import { useUpdateTenantSettings } from '../hooks/useUpdateTenantSettings';

const LOCALES = [
  { value: 'en', label: 'English' },
  { value: 'en-US', label: 'English (United States)' },
  { value: 'en-GB', label: 'English (United Kingdom)' },
  { value: 'de', label: 'German' },
  { value: 'fr', label: 'French' },
  { value: 'es', label: 'Spanish' },
  { value: 'it', label: 'Italian' },
  { value: 'nl', label: 'Dutch' },
  { value: 'pt', label: 'Portuguese' },
  { value: 'pt-BR', label: 'Portuguese (Brazil)' },
  { value: 'pl', label: 'Polish' },
  { value: 'cs', label: 'Czech' },
  { value: 'hr', label: 'Croatian' },
  { value: 'bs', label: 'Bosnian' },
  { value: 'sl', label: 'Slovenian' },
  { value: 'sr', label: 'Serbian' },
];

const TIMEZONES = [
  'UTC',
  'Europe/London',
  'Europe/Paris',
  'Europe/Berlin',
  'Europe/Amsterdam',
  'Europe/Madrid',
  'Europe/Rome',
  'Europe/Warsaw',
  'Europe/Prague',
  'Europe/Budapest',
  'Europe/Bucharest',
  'Europe/Istanbul',
  'Europe/Moscow',
  'Europe/Zagreb',
  'Europe/Sarajevo',
  'Europe/Ljubljana',
  'Europe/Belgrade',
  'America/New_York',
  'America/Chicago',
  'America/Denver',
  'America/Los_Angeles',
  'America/Toronto',
  'America/Vancouver',
  'America/Sao_Paulo',
  'Asia/Dubai',
  'Asia/Kolkata',
  'Asia/Singapore',
  'Asia/Shanghai',
  'Asia/Tokyo',
  'Asia/Seoul',
  'Australia/Sydney',
  'Australia/Melbourne',
  'Pacific/Auckland',
];

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

  function handleChange(field: 'locale' | 'timezone', value: string) {
    if (field === 'locale') setLocale(value);
    else setTimezone(value);
    if (updateSettings.isSuccess || updateSettings.isError) updateSettings.reset();
  }

  const isFormValid = locale.length > 0 && timezone.length > 0;

  return (
    <Box>
      <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
        Locale &amp; Timezone
      </Typography>

      {isLoading && (
        <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
          <Box sx={{ flex: 1 }}>
            <Grid container spacing={2}>
              <Grid size={4}>
                <Skeleton variant="rounded" height={56} />
              </Grid>
              <Grid size={4}>
                <Skeleton variant="rounded" height={56} />
              </Grid>
            </Grid>
          </Box>
          <Skeleton variant="rounded" width={64} height={36} />
        </Stack>
      )}

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load settings. Please try again.
        </Alert>
      )}

      {settings && (
        <Box>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
            <Box sx={{ flex: 1 }}>
              <Grid container spacing={2}>
                <Grid size={4}>
                  <FormControl fullWidth>
                    <InputLabel>Locale</InputLabel>
                    <Select
                      value={locale}
                      label="Locale"
                      onChange={(e) => handleChange('locale', e.target.value)}
                    >
                      {LOCALES.map((l) => (
                        <MenuItem key={l.value} value={l.value}>
                          {l.label}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>
                <Grid size={4}>
                  <FormControl fullWidth>
                    <InputLabel>Timezone</InputLabel>
                    <Select
                      value={timezone}
                      label="Timezone"
                      onChange={(e) => handleChange('timezone', e.target.value)}
                    >
                      {TIMEZONES.map((tz) => (
                        <MenuItem key={tz} value={tz}>
                          {tz}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>
              </Grid>

              {updateSettings.isError && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {getApiErrorMessage(updateSettings.error, 'Failed to save settings.')}
                </Alert>
              )}
              {updateSettings.isSuccess && (
                <Alert severity="success" sx={{ mt: 2 }}>
                  Settings saved.
                </Alert>
              )}
            </Box>

            <Button
              variant="contained"
              disabled={!isFormValid || updateSettings.isPending}
              onClick={() =>
                updateSettings.mutate({ locale: locale.trim(), timezone: timezone.trim() })
              }
            >
              Save
            </Button>
          </Stack>
        </Box>
      )}
    </Box>
  );
}
