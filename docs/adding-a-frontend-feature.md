# Adding a frontend feature

A quick reference for adding a new page/feature to the React frontend.

## File structure

```
src/frontend/src/
  pages/          # Top-level route components (one file per route)
  features/       # Feature-specific components, hooks, and helpers
    <feature>/
      components/ # UI components used only by this feature
      hooks/      # Custom hooks (data fetching, local state)
      types.ts    # Request/response types matching backend DTOs
  components/     # Shared UI components used across features
  lib/
    apiClient.ts  # Axios instance — import this for all HTTP calls
```

## 1. Add a route

Open `src/App.tsx` and add a `<Route>` inside the existing router. Use `<ProtectedRoute>` to require authentication, passing `requiredRole` for admin-only pages:

```tsx
// public page
<Route path="/my-page" element={<MyPage />} />

// requires any authenticated user
<Route path="/my-page" element={<ProtectedRoute><MyPage /></ProtectedRoute>} />

// requires the TenantAdmin role
<Route path="/my-page" element={<ProtectedRoute requiredRole={Roles.Admin}><MyPage /></ProtectedRoute>} />
```

Role constants live in `src/auth/roles.ts`.

## 2. Add API calls

Create `src/features/<feature>/hooks/useMyFeature.ts`. Use TanStack Query for data fetching and mutations:

```ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../../lib/apiClient';
import type { MyItem, CreateMyItemRequest } from '../types';

export function useMyItems() {
  return useQuery({
    queryKey: ['my-items'],
    queryFn: () => apiClient.get<MyItem[]>('/api/my-items').then(r => r.data),
  });
}

export function useCreateMyItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateMyItemRequest) =>
      apiClient.post<MyItem>('/api/my-items', req).then(r => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-items'] }),
  });
}
```

## 3. Add types

Mirror the backend DTOs in `src/features/<feature>/types.ts`:

```ts
export interface MyItem {
  id: string;
  name: string;
  createdAtUtc: string;
}

export interface CreateMyItemRequest {
  name: string;
}
```

## 4. Add the page

Create `src/pages/MyPage.tsx`:

```tsx
import { CircularProgress, Typography } from '@mui/material';
import { useMyItems } from '../features/my-feature/hooks/useMyFeature';

export default function MyPage() {
  const { data, isLoading, error } = useMyItems();

  if (isLoading) return <CircularProgress />;
  if (error) return <Typography color="error">Failed to load.</Typography>;

  return (
    <>
      {data?.map(item => (
        <div key={item.id}>{item.name}</div>
      ))}
    </>
  );
}
```

## 5. Add a nav link (optional)

If the page should appear in the sidebar, add it to the navigation list in `src/components/Layout.tsx` (or wherever the nav items are defined in your version).
