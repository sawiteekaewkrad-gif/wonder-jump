/// <reference types="vitest/config" />
import { defineConfig } from 'vite';

export default defineConfig({
  // Relative asset paths: the same build works on a web server and inside the Capacitor APK.
  base: './',
  define: {
    __APP_VERSION__: JSON.stringify(process.env.APP_VERSION_NAME ?? '0.1.0-dev'),
  },
  build: {
    target: 'es2022',
    sourcemap: true,
    chunkSizeWarningLimit: 900,
    rollupOptions: {
      output: { manualChunks: { three: ['three'] } },
    },
  },
  test: {
    environment: 'node',
    include: ['test/**/*.test.ts'],
  },
});
