import { useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import AddOutlinedIcon from '@mui/icons-material/AddOutlined';
import { mockTenants } from '../data/mock-tenants';
import type { TenantOrder } from '../types/tenant';
import { applyTenantFilter, getComparator } from '../utils';
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
  const [order, setOrder] = useState<TenantOrder>('asc');
  const [orderBy, setOrderBy] = useState<'name' | 'slug' | 'createdAtUtc' | 'isActive'>('name');
  const [selected, setSelected] = useState<string[]>([]);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(5);
  const [filterName, setFilterName] = useState('');

  const handleRequestSort = (property: string) => {
    const isAsc = orderBy === property && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(property as typeof orderBy);
  };

  const handleSelectAllClick = (checked: boolean) => {
    setSelected(checked ? mockTenants.map((tenant) => tenant.id) : []);
  };

  const handleSelectRow = (id: string) => {
    setSelected((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id],
    );
  };

  const filteredTenants = useMemo(
    () =>
      applyTenantFilter({
        tenants: mockTenants,
        comparator: getComparator(order, orderBy),
        query: filterName,
      }),
    [order, orderBy, filterName],
  );

  const paginatedTenants = filteredTenants.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage,
  );

  const isNotFound = filteredTenants.length === 0;

  return (
    <Card>
      <Stack
        direction="row"
        sx={{ alignItems: 'center', justifyContent: 'space-between', px: 3, pt: 3 }}
      >
        <Typography variant="h6">Tenants</Typography>
        <Button variant="contained" startIcon={<AddOutlinedIcon />} disabled>
          New tenant
        </Button>
      </Stack>

      <TenantTableToolbar
        numSelected={selected.length}
        filterName={filterName}
        onFilterName={(value) => {
          setFilterName(value);
          setPage(0);
        }}
      />

      <TableContainer sx={{ overflow: 'unset' }}>
        <Table sx={{ minWidth: 720 }}>
          <TenantTableHead
            order={order}
            orderBy={orderBy}
            numSelected={selected.length}
            rowCount={mockTenants.length}
            headCells={headCells}
            onRequestSort={handleRequestSort}
            onSelectAllClick={handleSelectAllClick}
          />
          <TableBody>
            {paginatedTenants.map((tenant) => (
              <TenantTableRow
                key={tenant.id}
                tenant={tenant}
                selected={selected.includes(tenant.id)}
                onSelectRow={() => handleSelectRow(tenant.id)}
              />
            ))}

            {isNotFound && (
              <TableRow>
                <TableCell colSpan={headCells.length + 1} align="center" sx={{ py: 6 }}>
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
    </Card>
  );
}
