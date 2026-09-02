import axios from 'axios';
import { apiClient } from '../../../lib/apiClient';
import type { Tenant } from '../../tenants/types/tenant';

export interface LogoUploadUrlResponse {
  uploadUrl: string;
  blobUrl: string;
}

export async function fetchLogoUploadUrl(contentType: string): Promise<LogoUploadUrlResponse> {
  const { data } = await apiClient.get<LogoUploadUrlResponse>('/tenants/me/logo/upload-url', {
    params: { contentType },
  });
  return data;
}

export async function uploadToBlob(uploadUrl: string, file: File): Promise<void> {
  await axios.put(uploadUrl, file, {
    headers: {
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type,
    },
  });
}

export async function confirmLogoUrl(logoUrl: string): Promise<Tenant> {
  const { data } = await apiClient.put<Tenant>('/tenants/me/logo', { logoUrl });
  return data;
}

export async function removeTenantLogo(): Promise<void> {
  await apiClient.delete('/tenants/me/logo');
}
