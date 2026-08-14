import axios from 'axios';

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
