export type TemplateFormat = 'liquid';

/**
 * Build a concrete props object whose values are Liquid placeholder strings.
 * Uses the keys of `sampleProps` as the source of truth.
 */
export function build_placeholders<T extends Record<string, unknown>>(
  sample: T,
): T {
  const out: Record<string, string> = {};
  for (const key of Object.keys(sample)) out[key] = `{{ ${key} }}`;
  return out as unknown as T;
}
