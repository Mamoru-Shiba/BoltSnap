import type { CaptureMode, CaptureProgress } from '@/types/capture';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControlLabel,
  Stack,
  Switch,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';

export interface CaptureMenuViewProps {
  mode: CaptureMode;
  copyToClipboard: boolean;
  progress: CaptureProgress;
  onModeChange: (mode: CaptureMode) => void;
  onCopyToClipboardChange: (value: boolean) => void;
  onCapture: () => void;
}

function ProgressLine({ progress }: { progress: CaptureProgress }): JSX.Element | null {
  if (progress.phase === 'idle') return null;
  if (progress.phase === 'error') {
    return (
      <Alert severity="error" sx={{ py: 0.5 }}>
        {progress.message}
      </Alert>
    );
  }
  if (progress.phase === 'done') {
    return (
      <Alert severity="success" sx={{ py: 0.5 }}>
        保存しました
      </Alert>
    );
  }
  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <CircularProgress size={16} />
      <Typography variant="body2">キャプチャ中…</Typography>
    </Stack>
  );
}

export function CaptureMenuView({
  mode,
  copyToClipboard,
  progress,
  onModeChange,
  onCopyToClipboardChange,
  onCapture,
}: CaptureMenuViewProps): JSX.Element {
  const isBusy =
    progress.phase === 'preparing' ||
    progress.phase === 'capturing' ||
    progress.phase === 'stitching' ||
    progress.phase === 'saving';

  return (
    <Box sx={{ width: 320, p: 2 }}>
      <Stack spacing={1.5}>
        <Typography variant="subtitle2">キャプチャモード</Typography>
        <ToggleButtonGroup
          value={mode}
          exclusive
          size="small"
          fullWidth
          onChange={(_, value): void => {
            if (value) onModeChange(value as CaptureMode);
          }}
        >
          <ToggleButton value="fullPage">フルページ</ToggleButton>
          <ToggleButton value="visible">可視領域</ToggleButton>
          <ToggleButton value="region">範囲選択</ToggleButton>
        </ToggleButtonGroup>

        <FormControlLabel
          control={
            <Switch
              checked={copyToClipboard}
              onChange={(e): void => onCopyToClipboardChange(e.target.checked)}
            />
          }
          label="クリップボードへコピー"
        />

        <Button variant="contained" disabled={isBusy} onClick={onCapture} fullWidth>
          キャプチャ
        </Button>

        <ProgressLine progress={progress} />
      </Stack>
    </Box>
  );
}
