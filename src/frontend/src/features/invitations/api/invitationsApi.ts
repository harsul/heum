import { apiClient } from '../../../lib/apiClient';
import type { Page } from '../../tenants/types/tenant';

export interface Invitation {
  id: string;
  email: string;
  status: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  acceptedAtUtc: string | null;
  revokedAtUtc: string | null;
}

export async function fetchInvitations(page: number, pageSize: number, search?: string): Promise<Page<Invitation>> {
  const { data } = await apiClient.get<Page<Invitation>>('/invitations', {
    params: { page, pageSize, search: search || undefined },
  });
  return data;
}

export async function createInvitation(email: string): Promise<Invitation> {
  const { data } = await apiClient.post<Invitation>('/invitations', { email });
  return data;
}

export async function revokeInvitation(id: string): Promise<void> {
  await apiClient.post(`/invitations/${id}/revoke`);
}

export async function acceptInvitation(token: string): Promise<void> {
  await apiClient.post('/invitations/accept', { token });
}
