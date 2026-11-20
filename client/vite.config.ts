import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,

    // The SPA calls /api on its own origin and Vite forwards it to the API. This
    // keeps the browser same-origin in development, so CORS and cookie behaviour
    // match production instead of being a dev-only special case.
    proxy: {
      '/api': {
        // Must match applicationUrl in the API's launchSettings.json.
        target: 'http://localhost:5090',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,

    rollupOptions: {
      output: {
        // Splits the vendor code out of one 950 kB bundle. These libraries change
        // only when we upgrade them, so a separate chunk stays in the browser
        // cache across deploys instead of being re-downloaded with every fix.
        // Charts are the heaviest and are used on two screens, so they get their
        // own chunk rather than loading on every page.
        manualChunks: {
          react: ['react', 'react-dom', 'react-router-dom'],
          mui: ['@mui/material', '@mui/icons-material', '@emotion/react', '@emotion/styled'],
          charts: ['@mui/x-charts'],
        },
      },
    },
  },
});
