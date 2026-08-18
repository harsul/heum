import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import IconButton from '@mui/material/IconButton';
import InputAdornment from '@mui/material/InputAdornment';
import OutlinedInput from '@mui/material/OutlinedInput';
import FilterListIcon from '@mui/icons-material/FilterListOutlined';
import SearchIcon from '@mui/icons-material/SearchOutlined';

interface TenantTableToolbarProps {
  filterName: string;
  onFilterName: (value: string) => void;
}

export function TenantTableToolbar({ filterName, onFilterName }: TenantTableToolbarProps) {
  return (
    <Toolbar
      sx={{
        height: 72,
        display: 'flex',
        justifyContent: 'space-between',
        p: (theme) => theme.spacing(0, 1, 0, 3),
      }}
    >
      <OutlinedInput
        value={filterName}
        onChange={(event) => onFilterName(event.target.value)}
        placeholder="Search tenant..."
        size="small"
        startAdornment={
          <InputAdornment position="start">
            <SearchIcon sx={{ color: 'text.disabled' }} fontSize="small" />
          </InputAdornment>
        }
        sx={{ width: { xs: 1, sm: 280 } }}
      />

      <Tooltip title="Filter list">
        <IconButton>
          <FilterListIcon />
        </IconButton>
      </Tooltip>
    </Toolbar>
  );
}
