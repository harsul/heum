import axios, { isAxiosError } from 'axios';
import { enqueueSnackbar } from 'notistack';

let _accessToken: string | undefined;

export function setAccessToken(token: string | undefined): void {
  _accessToken = token;
}

export const apiClient = axios.create({ baseURL: '/api' });

apiClient.interceptors.request.use((config) => {
  if (_accessToken) {
    config.headers.Authorization = `Bearer ${_accessToken}`;
  }
  return config;
});

apiClient.interceptors.response.use(undefined, (error: unknown) => {
  if (isAxiosError(error)) {
    const status = error.response?.status;
    const detail =
      (error.response?.data as { detail?: string; title?: string })?.detail ??
      (error.response?.data as { detail?: string; title?: string })?.title;

    if (status === 401) {
      enqueueSnackbar('Session expired. Please sign in again.', { variant: 'warning' });
    } else if (status === 403) {
      enqueueSnackbar('You don’t have permission to perform this action.', { variant: 'error' });
    } else if (status && status >= 500) {
      enqueueSnackbar(detail ?? 'Something went wrong. Please try again later.', { variant: 'error' });
    } else if (!error.response) {
      enqueueSnackbar('Network error. Check your connection.', { variant: 'error' });
    }
  }
  return Promise.reject(error);
});
