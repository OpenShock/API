# OpenShock SMTP Templates

The API's transactional email templates, authored as React components via
[`react-email`](https://react.email/) and exported to Liquid (`{{ name }}`
variables) for the Fluid-based `SmtpTemplate` runtime loader.

## How it builds

`dotnet build API/API.csproj` runs three MSBuild targets before compilation:

1. `InstallEmailTemplateDeps` — `pnpm install --frozen-lockfile` (only when
   `package.json` / `pnpm-lock.yaml` change).
2. `BuildEmailTemplates` — `pnpm export`, rendering each `emails/*.tsx` to
   `SmtpTemplates/dist/*.liquid` (only when any `.tsx` / `.ts` / lockfile
   changes).
3. `IncludeEmailTemplates` — the generated `.liquid` files are attached as
   `Content` items linked to `SmtpTemplates/*.liquid` in the build output, so
   the API can load them at runtime exactly as before.

`pnpm` must be on `PATH`. `dist/` and `node_modules/` are gitignored.

## Develop standalone

```sh
pnpm install
pnpm dev          # live preview at http://localhost:3000
pnpm export       # writes dist/*.liquid
```

Each exported file is a Fluid/Liquid template: line 1 is the subject, the rest
is the HTML body — the format expected by
`API/Services/Email/Smtp/SmtpTemplate.cs`.

## Authoring a template

Each `emails/*.tsx` file should export:

- a **named** PascalCase React component (the parameterised template),
- a `subject` string — emitted as the first line of the exported `.liquid` file (Fluid reads line 1 as the subject template),
- a `sampleProps` object — keys define which props get placeholder-substituted on export; values are used by the live preview,
- a **default** export wrapping the component with `sampleProps` so `email dev` can render it.

Example:

```tsx
export interface VerifyEmailProps {
  'To.Name': string;
  VerifyLink: string;
}

export const subject = 'Hi! Verify your Email!';

export const sampleProps: VerifyEmailProps = {
  'To.Name': 'shockee',
  VerifyLink: 'https://openshock.app/verify?token=preview',
};

export function EmailVerification(props: VerifyEmailProps) {
  /* ... */
}

export default function Preview_EmailVerification() {
  return <EmailVerification {...sampleProps} />;
}
```

On export, prop values become `{{ To.Name }}` / `{{ VerifyLink }}` etc. Prop
names must match the variable names the API passes into Fluid at send time.

## Layout

- `emails/*.tsx` — templates (files starting with `_` are ignored by the exporter).
- `emails/_lib/` — shared helpers.
- `scripts/export-templates.ts` — Liquid exporter.
- `dist/` — generated `.liquid` files (gitignored, produced by `pnpm export`).

## License

AGPL-3.0
