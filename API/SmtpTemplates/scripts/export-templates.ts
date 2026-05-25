import { render } from '@react-email/render';
import { readdir, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createElement } from 'react';
import { build_placeholders } from '../emails/_lib/placeholders.ts';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = resolve(__dirname, '..');
const emails_dir = join(root, 'emails');
const out_dir = root;

import type { ComponentType } from 'react';

type TemplateModule = {
  default?: unknown;
  sampleProps?: Record<string, unknown>;
  subject?: string;
  [key: string]: unknown;
};

function pick_component(mod: TemplateModule, file: string): ComponentType<object> {
  for (const [name, value] of Object.entries(mod)) {
    if (name === 'default' || name === 'sampleProps' || name === 'subject') continue;
    if (typeof value === 'function' && /^[A-Z]/.test(name)) {
      return value as ComponentType<object>;
    }
  }
  throw new Error(`No named PascalCase component export found in ${file}`);
}

async function list_templates(): Promise<string[]> {
  const entries = await readdir(emails_dir, { withFileTypes: true });
  return entries
    .filter(
      (e: { isFile(): boolean; name: string }) =>
        e.isFile() && e.name.endsWith('.tsx') && !e.name.startsWith('_'),
    )
    .map((e: { name: string }) => e.name);
}

async function export_liquid() {
  const files = await list_templates();
  let rendered = 0;
  for (const file of files) {
    const mod = (await import(
      pathToFileURL(join(emails_dir, file)).href
    )) as TemplateModule;
    if (!mod.sampleProps) {
      console.log(`  skip ${file} (no sampleProps export)`);
      continue;
    }
    if (!mod.subject) {
      console.log(`  skip ${file} (no subject export)`);
      continue;
    }
    const Component = pick_component(mod, file);
    const props = build_placeholders(mod.sampleProps);
    const body = await render(createElement(Component, props), { pretty: true });
    const out_name = file.replace(/\.tsx$/, '.liquid');
    await writeFile(join(out_dir, out_name), `${mod.subject}\n${body}`, 'utf8');
    console.log(`  ${out_name}`);
    rendered++;
  }
  console.log(`  ${rendered} template(s) rendered\n`);
}

console.log('Exporting liquid...');
await export_liquid();
