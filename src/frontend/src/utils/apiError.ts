import { isAxiosError } from 'axios';

export function getApiErrorMessage(error: unknown, fallback: string): string | undefined {
  if (!error) return undefined;

  if (isAxiosError<{ detail?: string; title?: string }>(error)) {
    return error.response?.data?.detail ?? error.response?.data?.title ?? fallback;
  }

  return fallback;
}
