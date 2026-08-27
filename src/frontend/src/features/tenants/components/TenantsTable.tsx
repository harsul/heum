import { useMemo, useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import AddOutlinedIcon from '@mui/icons-material/AddOutlined';
import type { Tenant, TenantOrder } from '../types/tenant';
import { applyTenantFilter, getComparator } from '../utils';
import { getApiErrorMessage } from '../../../utils/apiError';
import { useTenants } from '../hooks/useTenants';
import { useCreateTenant } from '../hooks/useCreateTenant';
import { useSetTenantActive, useUpdateTenant } from '../hooks/useTenantMutations';
import { EditTenantDialog } from './EditTenantDialog';
import { NewTenantDialog } from './NewTenantDialog';
import { TenantTableHead, type HeadCell } from './TenantTableHead';
import { TenantTableRow } from './TenantTableRow';
import { TenantTableToolbar } from './TenantTableToolbar';

const headCells: HeadCell[] = [
  { id: 'name', label: 'Tenant' },
  { id: 'slug', label: 'Slug' },
  { id: 'createdAtUtc', label: 'Created' },
  { id: 'updatedAtUtc', label: 'Updated', sortable: false },
  { id: 'isActive', label: 'Status', align: 'center' },
  { id: 'actions', label: '', sortable: false },
];

export function TenantsTable() {
  const { data: tenants = [], isLoading, isError } = useTenants();
  const updateTenant = useUpdateTenant();
  const setTenantActive = useSetTenantActive();
  const createTenant = useCreateTenant();

  const [order, setOrder] = useState<TenantOrder>('asc');
  const [orderBy, setOrderBy] = useState<'name' | 'slug' | 'createdAtUtc' | 'isActive'>('name');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(5);
  const [filterName, setFilterName] = useState('');
  const [editingTenant, setEditingTenant] = useState<Tenant | null>(null);
  const [isNewTenantOpen, setIsNewTenantOpen] = useState(false);

  const handleRequestSort = (property: string) => {
    const isAsc = orderBy === property && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(property as typeof orderBy);
  };

  const filteredTenants = useMemo(
    () =>
      applyTenantFilter({
        tenants,
        comparator: getComparator(order, orderBy),
        query: filterName,
      }),
    [tenants, order, orderBy, filterName],
  );

  const paginatedTenants = filteredTenants.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage,
  );

  const isNotFound = !isLoading && !isError && filteredTenants.length === 0;

  return (
    <Card>
      <Stack
        direction="row"
        sx={{ alignItems: 'center', justifyContent: 'space-between', px: 3, pt: 3 }}
      >
        <Box>
          <Typography variant="h6">Tenants</Typography>
          <Typography variant="body2" color="text.secondary">Companies onboarded to your platform.</Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddOutlinedIcon />}
          onClick={() => {
            createTenant.reset();
            setIsNewTenantOpen(true);
          }}
        >
          New tenant
        </Button>
      </Stack>

      <TenantTableToolbar
        filterName={filterName}
        onFilterName={(value) => {
          setFilterName(value);
          setPage(0);
        }}
      />

      {isError && (
        <Alert severity="error" sx={{ mx: 3, mb: 2 }}>
          Failed to load tenants. Please try again.
        </Alert>
      )}

      <TableContainer sx={{ overflow: 'unset' }}>
        <Table sx={{ minWidth: 720 }}>
          <TenantTableHead
            order={order}
            orderBy={orderBy}
            headCells={headCells}
            onRequestSort={handleRequestSort}
          />
          <TableBody>
            {isLoading &&
              Array.from({ length: 5 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell>
                    <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                      <Skeleton variant="circular" width={40} height={40} />
                      <Skeleton variant="text" width={120} />
                    </Stack>
                  </TableCell>
                  <TableCell><Skeleton variant="text" width={80} /></TableCell>
                  <TableCell><Skeleton variant="text" width={90} /></TableCell>
                  <TableCell><Skeleton variant="text" width={90} /></TableCell>
                  <TableCell align="center"><Skeleton variant="rounded" width={56} height={24} /></TableCell>
                  <TableCell><Skeleton variant="circular" width={28} height={28} /></TableCell>
                </TableRow>
              ))}

            {!isLoading &&
              paginatedTenants.map((tenant) => (
                <TenantTableRow
                  key={tenant.id}
                  tenant={tenant}
                  onEdit={() => setEditingTenant(tenant)}
                  onToggleActive={() =>
                    setTenantActive.mutate({ id: tenant.id, isActive: !tenant.isActive })
                  }
                  toggleActiveDisabled={setTenantActive.isPending}
                />
              ))}

            {isNotFound && (
              <TableRow>
                <TableCell colSpan={headCells.length} align="center" sx={{ py: 6 }}>
                  <Box>
                    <Typography variant="subtitle1">No tenants found</Typography>
                    <Typography variant="body2" color="text.secondary">
                      No results found for &quot;{filterName}&quot;. Try a different search.
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <TablePagination
        component="div"
        count={filteredTenants.length}
        page={page}
        onPageChange={(_, newPage) => setPage(newPage)}
        rowsPerPage={rowsPerPage}
        onRowsPerPageChange={(event) => {
          setRowsPerPage(parseInt(event.target.value, 10));
          setPage(0);
        }}
        rowsPerPageOptions={[5, 10, 25]}
      />

      <EditTenantDialog
        tenant={editingTenant}
        open={editingTenant !== null}
        saving={updateTenant.isPending}
        onClose={() => setEditingTenant(null)}
        onSave={(values) => {
          if (!editingTenant) return;
          updateTenant.mutate(
            { id: editingTenant.id, payload: values },
            { onSuccess: () => setEditingTenant(null) },
          );
        }}
      />

      <NewTenantDialog
        open={isNewTenantOpen}
        saving={createTenant.isPending}
        errorMessage={getApiErrorMessage(createTenant.error, 'Failed to create tenant.')}
        onClose={() => setIsNewTenantOpen(false)}
        onCreate={(values) => {
          createTenant.mutate(values, { onSuccess: () => setIsNewTenantOpen(false) });
        }}
      />
    </Card>
  );
}
