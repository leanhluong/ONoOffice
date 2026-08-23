// @ts-check
const eslint = require('@eslint/js');
const { defineConfig, globalIgnores } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const prettier = require('eslint-config-prettier/flat');

/**
 * Phân công rõ ràng giữa hai công cụ:
 * - Prettier lo HÌNH THỨC (xuống dòng, dấu nháy, dấu phẩy cuối).
 * - ESLint lo NỘI DUNG (dùng sai API, biến thừa, vi phạm quy ước Angular).
 *
 * `eslint-config-prettier` phải đặt CUỐI mảng extends: nhiệm vụ của nó là tắt
 * mọi rule ESLint đụng tới định dạng, nếu đặt trước thì các preset sau lại bật
 * chúng lên và hai bên sẽ đánh nhau ở mỗi lần lưu file.
 */
module.exports = defineConfig([
  globalIgnores(['dist/**', 'coverage/**', '.angular/**', 'node_modules/**']),
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
      prettier,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      // Toàn bộ selector của app dùng tiền tố `app-` để không đụng tên với
      // thư viện bên thứ ba nhúng sau này.
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'app', style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'app', style: 'kebab-case' },
      ],
      // Cấm `any`: khi hợp đồng API còn chưa chốt, `unknown` buộc phải kiểm
      // kiểu trước khi dùng — đó chính là chỗ dễ sinh lỗi lúc backend đổi field.
      '@typescript-eslint/no-explicit-any': 'error',
      // Biến không dùng là dấu hiệu code chết hoặc quên xoá; cho phép đặt tên
      // bắt đầu bằng `_` để cố ý bỏ qua tham số (ví dụ `_route` trong guard).
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
      // Component/service chỉ nên console.error hoặc warn; console.log lọt lên
      // production là rác, có khi còn lộ dữ liệu người dùng.
      'no-console': ['warn', { allow: ['warn', 'error'] }],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility, prettier],
    rules: {},
  },
]);
