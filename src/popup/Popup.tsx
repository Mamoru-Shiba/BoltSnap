import { CaptureMenu } from '@/components/features/capture-menu';
import { CssBaseline, ThemeProvider, createTheme } from '@mui/material';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#4A90D9' },
  },
  typography: {
    fontFamily: 'system-ui, -apple-system, sans-serif',
  },
});

export function Popup(): JSX.Element {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <CaptureMenu />
    </ThemeProvider>
  );
}
