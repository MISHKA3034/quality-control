import { useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Marks the action as destructive, colouring the confirm button red. */
  destructive?: boolean;
  onConfirm: () => void | Promise<void>;
  onClose: () => void;
}

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Подтвердить',
  cancelLabel = 'Отмена',
  destructive = false,
  onConfirm,
  onClose,
}: ConfirmDialogProps) {
  const [isBusy, setIsBusy] = useState(false);

  async function handleConfirm() {
    setIsBusy(true);

    try {
      await onConfirm();
      onClose();
    } finally {
      // Also runs when onConfirm throws, so a failed action leaves the dialog
      // usable instead of stuck on a disabled button.
      setIsBusy(false);
    }
  }

  return (
    <Dialog open={open} onClose={isBusy ? undefined : onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{title}</DialogTitle>

      <DialogContent>
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} disabled={isBusy}>
          {cancelLabel}
        </Button>

        <Button
          onClick={() => void handleConfirm()}
          variant="contained"
          color={destructive ? 'error' : 'primary'}
          disabled={isBusy}
        >
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
