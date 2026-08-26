import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

interface DetailFieldProps {
  label: string;
  value: React.ReactNode;
}

export function DetailField({ label, value }: DetailFieldProps) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 600 }}>{value}</Typography>
    </Box>
  );
}
