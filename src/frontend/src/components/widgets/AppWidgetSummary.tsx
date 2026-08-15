import Card from '@mui/material/Card';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import type { SvgIconComponent } from '@mui/icons-material';
import type { SxProps, Theme } from '@mui/material/styles';

interface AppWidgetSummaryProps {
  title: string;
  total: string | number;
  icon: SvgIconComponent;
  color?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  sx?: SxProps<Theme>;
}

export function AppWidgetSummary({
  title,
  total,
  icon: Icon,
  color = 'primary',
  sx,
}: AppWidgetSummaryProps) {
  return (
    <Card
      sx={{
        p: 3,
        display: 'flex',
        alignItems: 'center',
        gap: 2,
        ...sx,
      }}
    >
      <Box
        sx={{
          width: 56,
          height: 56,
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexShrink: 0,
          bgcolor: (theme) => `${theme.palette[color].main}26`,
          color: `${color}.main`,
        }}
      >
        <Icon fontSize="medium" />
      </Box>

      <Box>
        <Typography variant="h4" sx={{ fontWeight: 700, lineHeight: 1.2 }}>
          {total}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {title}
        </Typography>
      </Box>
    </Card>
  );
}
