import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import IconButton from '@mui/material/IconButton';
import Typography from '@mui/material/Typography';
import InputAdornment from '@mui/material/InputAdornment';
import OutlinedInput from '@mui/material/OutlinedInput';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlineOutlined';
import FilterListIcon from '@mui/icons-material/FilterListOutlined';
import SearchIcon from '@mui/icons-material/SearchOutlined';

interface TenantTableToolbarProps {
  numSelected: number;
  filterName: string;
  onFilterName: (value: string) => void;
}

export function TenantTableToolbar({
  numSelected,
  filterName,
  onFilterName,
}: TenantTableToolbarProps) {
  return (
    <Toolbar
      sx={{
        height: 72,
        display: 'flex',
        justifyContent: 'space-between',
        p: (theme) => theme.spacing(0, 1, 0, 3),
        ...(numSelected > 0 && {
          color: 'primary.main',
          bgcolor: 'action.selected',
        }),
      }}
    >
      {numSelected > 0 ? (
        <Typography component="div" variant="subtitle1">
          {numSelected} selected
        </Typography>
      ) : (
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
      )}

      {numSelected > 0 ? (
        <Tooltip title="Delete">
          <IconButton>
            <DeleteOutlineIcon />
          </IconButton>
        </Tooltip>
      ) : (
        <Tooltip title="Filter list">
          <IconButton>
            <FilterListIcon />
          </IconButton>
        </Tooltip>
      )}
    </Toolbar>
  );
}
