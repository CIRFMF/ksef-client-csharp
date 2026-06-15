import path from 'path';
import { defineConfig } from 'vite';
import dts from 'vite-plugin-dts';

export default defineConfig(({ mode }) => {
  const libRoot = 'src';
  const appRoot = 'src/app-public';

  return {
    root: mode === 'production' ? '' : appRoot,
    build: {
      lib: {
        name: 'ksef-fe-invoice-converter',
        entry: path.resolve(__dirname, libRoot, 'index.ts'),
        outDir: path.resolve(__dirname, 'dist'),
        emptyOutDir: true,
        formats: ['es', 'umd'],
        rollupOptions: {
          external: [/\.spec\.ts$/, /\.test\.ts$/, 'src/app-private', 'src/app-public'],
        },
      },
    },

    test: {
      root: __dirname,
      globals: true,
      environment: 'jsdom',
      include: ['src/**/*.spec.{ts,tsx}', 'src/**/*.test.{ts,tsx}'],
      exclude: [
        'node_modules',
        'dist',
        'cypress',
        '**/{karma,rollup,webpack,vite,vitest,jest,ava,babel,nyc,cypress,tsup,build,eslint,prettier}.config.*',
      ],
      coverage: {
        provider: 'v8',
        reporter: ['text-summary', 'lcov', 'html'],
      },
    },

    plugins: [
      dts({
        entryRoot: libRoot,
        insertTypesEntry: true,
        outDir: path.resolve(__dirname, 'dist'),
        exclude: ['src/app-public'],
      }),
    ],

    server: {
      port: 5173,
    },
  };
});
