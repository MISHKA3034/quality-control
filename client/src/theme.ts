import { createTheme } from '@mui/material/styles';
import { ruRU } from '@mui/material/locale';

/**
 * Application theme.
 *
 * Industrial rather than playful: this is a tool people use for a whole shift.
 * Dense tables, restrained colour, and status colours reserved for status — so a
 * red chip always means a real problem.
 */
export const theme = createTheme(
  {
    palette: {
      primary: { main: '#1B4F72', light: '#2E6DA4', dark: '#123651' },
      secondary: { main: '#5D6D7E' },
      success: { main: '#1E8449' },
      warning: { main: '#B9770E' },
      error: { main: '#B03A2E' },
      info: { main: '#2874A6' },
      background: { default: '#F4F6F8', paper: '#FFFFFF' },
    },

    typography: {
      fontFamily: '"Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
      h4: { fontWeight: 600, fontSize: '1.6rem' },
      h5: { fontWeight: 600, fontSize: '1.25rem' },
      h6: { fontWeight: 600, fontSize: '1.05rem' },
      button: { textTransform: 'none', fontWeight: 500 },
    },

    shape: { borderRadius: 6 },

    components: {
      MuiTableCell: {
        styleOverrides: {
          root: { paddingTop: 8, paddingBottom: 8 },
          head: { fontWeight: 600, backgroundColor: '#EDF1F5', whiteSpace: 'nowrap' },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: { border: '1px solid #E3E8EF', boxShadow: 'none' },
        },
      },
      MuiChip: {
        styleOverrides: { root: { fontWeight: 500 } },
      },
      MuiButton: {
        defaultProps: { disableElevation: true },
      },
      MuiTextField: {
        defaultProps: { size: 'small' },
      },
      MuiSelect: {
        defaultProps: { size: 'small' },
      },
    },
  },

  // Localises MUI's own strings (pagination, selects) to Russian.
  ruRU,
);
