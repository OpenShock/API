import type { CSSProperties } from 'react';

const fontStack =
  'ui-sans-serif, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';

const monoStack =
  'ui-monospace, SFMono-Regular, Menlo, Consolas, "Liberation Mono", monospace';

const colors = {
  pageBg: '#f4f4f5',
  card: '#ffffff',
  foreground: '#09090b',
  mutedForeground: '#71717a',
  border: '#e4e4e7',
  muted: '#f4f4f5',
  primary: '#18181b',
  primaryForeground: '#fafafa',
  codeBg: '#f4f4f5',
} as const;

export const styles = {
  body: {
    fontFamily: fontStack,
    lineHeight: '1.6',
    backgroundColor: colors.pageBg,
    color: colors.foreground,
    margin: '0',
    padding: '0',
    WebkitFontSmoothing: 'antialiased',
  },
  container: {
    maxWidth: '560px',
    margin: '32px auto',
    backgroundColor: colors.card,
    border: `1px solid ${colors.border}`,
    borderRadius: '12px',
    overflow: 'hidden',
  },
  header: {
    padding: '24px 32px',
    backgroundColor: colors.primary,
  },
  brandLogo: {
    display: 'block',
    margin: '0 auto',
    // Styles below apply to the `alt` text when the image is blocked,
    // so the OpenShock wordmark still looks like a header in that case.
    fontSize: '20px',
    fontWeight: '700',
    letterSpacing: '0.5px',
    color: colors.accentText,
    textDecoration: 'none',
  },
  content: {
    padding: '28px 32px 8px',
  },
  heading: {
    color: colors.foreground,
    fontSize: '22px',
    fontWeight: '600',
    margin: '0 0 16px',
    lineHeight: '1.3',
    letterSpacing: '-0.025em',
  },
  text: {
    fontSize: '14px',
    color: colors.foreground,
    margin: '0 0 16px',
    lineHeight: '1.7',
  },
  muted: {
    fontSize: '13px',
    color: colors.mutedForeground,
    margin: '0 0 12px',
    lineHeight: '1.6',
  },
  buttonSection: {
    padding: '8px 0 20px',
  },
  button: {
    display: 'inline-block',
    padding: '10px 20px',
    backgroundColor: colors.primary,
    color: colors.primaryForeground,
    fontSize: '14px',
    fontWeight: '500',
    textDecoration: 'none',
    borderRadius: '6px',
  },
  rawLinkLabel: {
    fontSize: '13px',
    color: colors.mutedForeground,
    margin: '0 0 6px',
  },
  rawLink: {
    fontFamily: monoStack,
    fontSize: '12px',
    color: colors.mutedForeground,
    wordBreak: 'break-all',
    margin: '0 0 20px',
    display: 'block',
  },
  inlineCode: {
    fontFamily: monoStack,
    backgroundColor: colors.codeBg,
    padding: '2px 6px',
    borderRadius: '4px',
    fontSize: '13px',
  },
  notice: {
    backgroundColor: colors.muted,
    border: `1px solid ${colors.border}`,
    borderRadius: '8px',
    padding: '12px 16px',
    margin: '0 0 20px',
    fontSize: '13px',
    color: colors.mutedForeground,
    lineHeight: '1.6',
  },
  divider: {
    border: 'none',
    borderTop: `1px solid ${colors.border}`,
    margin: '24px 0',
  },
  footer: {
    padding: '20px 32px 24px',
    backgroundColor: colors.muted,
    borderTop: `1px solid ${colors.border}`,
  },
  footerText: {
    fontSize: '12px',
    color: colors.mutedForeground,
    margin: '0 0 6px',
    lineHeight: '1.5',
  },
} satisfies Record<string, CSSProperties>;
