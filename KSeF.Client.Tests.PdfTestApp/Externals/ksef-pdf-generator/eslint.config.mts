// @ts-check
import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';
import eslintPluginPrettierRecommended from 'eslint-plugin-prettier/recommended';
import stylistics from '@stylistic/eslint-plugin';

module.exports = tseslint.config({
  ignores: [
    'package.json',
    'node_modules',
    'package-lock.json',
    'dist',
    'angular.json',
    'tsconfig.json',
    'tsconfig.app.json',
    'tsconfig.spec.json',
    'src/types**/*.ts',
  ],
  files: ['**/*.ts'],
  extends: [
    eslint.configs.recommended,
    ...tseslint.configs.recommended,
    ...tseslint.configs.stylistic,
    eslintPluginPrettierRecommended,
  ],
  plugins: {
    '@stylistic': stylistics,
  },
  rules: {
    '@typescript-eslint/no-explicit-any': 'off',
    'prettier/prettier': [
      'error',
      {
        endOfLine: 'auto',
      },
    ],
    '@typescript-eslint/explicit-member-accessibility': [
      'warn',
      {
        accessibility: 'explicit',
        overrides: {
          constructors: 'no-public',
          accessors: 'off',
        },
      },
    ],
    '@typescript-eslint/member-ordering': ['warn'],
    '@typescript-eslint/explicit-function-return-type': ['error'],
    curly: ['error', 'all'],
    '@stylistic/padding-line-between-statements': [
      'error',
      { blankLine: 'always', prev: ['const', 'let', 'var'], next: '*' },
      {
        blankLine: 'any',
        prev: ['const', 'let', 'var'],
        next: ['const', 'let', 'var'],
      },
    ],
  },
});
