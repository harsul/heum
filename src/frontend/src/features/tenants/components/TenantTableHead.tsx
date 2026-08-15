import Box from '@mui/material/Box';
import Checkbox from '@mui/material/Checkbox';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TableSortLabel from '@mui/material/TableSortLabel';
import type { TenantOrder } from '../types/tenant';

export interface HeadCell {
  id: string;
  label: string;
  sortable?: boolean;
  align?: 'left' | 'right' | 'center';
}

interface TenantTableHeadProps {
  order: TenantOrder;
  orderBy: string;
  numSelected: number;
  rowCount: number;
  headCells: HeadCell[];
  onRequestSort: (property: string) => void;
  onSelectAllClick: (checked: boolean) => void;
}

export function TenantTableHead({
  order,
  orderBy,
  numSelected,
  rowCount,
  headCells,
  onRequestSort,
  onSelectAllClick,
}: TenantTableHeadProps) {
  return (
    <TableHead>
      <TableRow>
        <TableCell padding="checkbox">
          <Checkbox
            indeterminate={numSelected > 0 && numSelected < rowCount}
            checked={rowCount > 0 && numSelected === rowCount}
            onChange={(event) => onSelectAllClick(event.target.checked)}
          />
        </TableCell>
        {headCells.map((headCell) => (
          <TableCell
            key={headCell.id}
            align={headCell.align ?? 'left'}
            sortDirection={orderBy === headCell.id ? order : false}
          >
            {headCell.sortable === false ? (
              headCell.label
            ) : (
              <TableSortLabel
                hideSortIcon
                active={orderBy === headCell.id}
                direction={orderBy === headCell.id ? order : 'asc'}
                onClick={() => onRequestSort(headCell.id)}
              >
                {headCell.label}
                {orderBy === headCell.id ? (
                  <Box component="span" sx={visuallyHidden}>
                    {order === 'desc' ? 'sorted descending' : 'sorted ascending'}
                  </Box>
                ) : null}
              </TableSortLabel>
            )}
          </TableCell>
        ))}
      </TableRow>
    </TableHead>
  );
}

const visuallyHidden = {
  border: 0,
  clip: 'rect(0 0 0 0)',
  height: 1,
  margin: -1,
  overflow: 'hidden',
  padding: 0,
  position: 'absolute',
  width: 1,
} as const;
