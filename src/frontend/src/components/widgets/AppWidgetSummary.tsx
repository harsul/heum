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
        transition: 'box-shadow 0.2s ease, transform 0.2s ease',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: '0 0 2px 0 rgba(145,158,171,0.24), 0 16px 32px -4px rgba(145,158,171,0.24)',
        },
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
