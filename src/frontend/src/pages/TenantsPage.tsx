import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { TenantsTable } from '../features/tenants/components';

export function TenantsPage() {
  return (
    <DashboardLayout>
      <TenantsTable />
    </DashboardLayout>
  );
}
