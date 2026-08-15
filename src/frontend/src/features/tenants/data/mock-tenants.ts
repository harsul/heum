import type { Tenant } from '../types/tenant';

/**
 * Temporary in-memory data standing in for `GET /api/tenants`.
 * Replace with a real API call (e.g. via TanStack Query) once the
 * server exposes a tenants listing endpoint.
 */
export const mockTenants: Tenant[] = [
  {
    id: '8f14e2b0-1a3c-4b7e-9c3b-1f2a3b4c5d6e',
    name: 'Acme Corporation',
    slug: 'acme',
    isActive: true,
    createdAtUtc: '2025-11-02T09:15:00Z',
    updatedAtUtc: '2026-06-18T14:20:00Z',
  },
  {
    id: '2b6d9e1a-4f3c-4d2e-8a1b-6c7d8e9f0a1b',
    name: 'Globex Industries',
    slug: 'globex',
    isActive: true,
    createdAtUtc: '2025-12-14T11:42:00Z',
    updatedAtUtc: null,
  },
  {
    id: '5c3a7f2d-9b1e-4a6c-8d2f-3e4f5a6b7c8d',
    name: 'Initech Solutions',
    slug: 'initech',
    isActive: false,
    createdAtUtc: '2026-01-09T08:05:00Z',
    updatedAtUtc: '2026-03-22T10:11:00Z',
  },
  {
    id: '9d1f4e6a-2c8b-4f3d-9a1c-5b6d7e8f9a0b',
    name: 'Umbrella Group',
    slug: 'umbrella',
    isActive: true,
    createdAtUtc: '2026-02-27T16:30:00Z',
    updatedAtUtc: null,
  },
  {
    id: '3e7c1a9d-6f2b-4e5c-8d3a-9b0c1d2e3f4a',
    name: 'Wayne Enterprises',
    slug: 'wayne-enterprises',
    isActive: true,
    createdAtUtc: '2026-03-05T13:00:00Z',
    updatedAtUtc: '2026-07-01T09:45:00Z',
  },
  {
    id: '6a2d8f1c-3b9e-4c7a-9f1b-2d3e4f5a6b7c',
    name: 'Stark Industries',
    slug: 'stark',
    isActive: false,
    createdAtUtc: '2026-03-19T07:22:00Z',
    updatedAtUtc: null,
  },
  {
    id: '1f9b3d5e-7a2c-4b8d-9e1f-4a5b6c7d8e9f',
    name: 'Hooli',
    slug: 'hooli',
    isActive: true,
    createdAtUtc: '2026-04-11T12:18:00Z',
    updatedAtUtc: '2026-05-30T15:52:00Z',
  },
  {
    id: '4c8e2a6f-1d9b-4e3c-8a2d-5f6a7b8c9d0e',
    name: 'Soylent Corp',
    slug: 'soylent',
    isActive: true,
    createdAtUtc: '2026-04-28T10:40:00Z',
    updatedAtUtc: null,
  },
];
