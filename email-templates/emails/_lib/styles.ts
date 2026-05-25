import type { CSSProperties } from 'react';

const fontStack =
  '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';

const colors = {
  pageBg: '#f4f5f7',
  cardBg: '#ffffff',
  border: '#e4e7eb',
  text: '#1f2329',
  textMuted: '#5a6473',
  textFaint: '#8a93a3',
  accent: '#111827',
  accentText: '#ffffff',
  noticeBg: '#fff8e1',
  noticeBorder: '#f0c419',
  noticeText: '#5a4500',
  codeBg: '#f1f3f5',
  link: '#1f6feb',
} as const;

export const styles = {
  body: {
    fontFamily: fontStack,
    lineHeight: '1.6',
    backgroundColor: colors.pageBg,
    color: colors.text,
    margin: '0',
    padding: '0',
    WebkitFontSmoothing: 'antialiased',
  },
  container: {
    maxWidth: '560px',
    margin: '32px auto',
    backgroundColor: colors.cardBg,
    border: `1px solid ${colors.border}`,
    borderRadius: '8px',
    overflow: 'hidden',
  },
  header: {
    padding: '20px 32px',
    backgroundColor: colors.accent,
    color: colors.accentText,
  },
  brand: {
    margin: '0',
    fontSize: '18px',
    fontWeight: '700',
    letterSpacing: '0.5px',
    color: colors.accentText,
  },
  content: {
    padding: '28px 32px 8px',
  },
  heading: {
    color: colors.text,
    fontSize: '22px',
    fontWeight: '600',
    margin: '0 0 16px',
    lineHeight: '1.3',
  },
  text: {
    fontSize: '15px',
    color: colors.text,
    margin: '0 0 16px',
  },
  muted: {
    fontSize: '13px',
    color: colors.textMuted,
    margin: '0 0 12px',
  },
  buttonSection: {
    padding: '8px 0 20px',
  },
  button: {
    display: 'inline-block',
    padding: '12px 24px',
    backgroundColor: colors.accent,
    color: colors.accentText,
    fontSize: '15px',
    fontWeight: '600',
    textDecoration: 'none',
    borderRadius: '6px',
  },
  rawLinkLabel: {
    fontSize: '13px',
    color: colors.textMuted,
    margin: '0 0 6px',
  },
  rawLink: {
    fontFamily:
      'ui-monospace, SFMono-Regular, Menlo, Consolas, "Liberation Mono", monospace',
    fontSize: '12px',
    color: colors.link,
    wordBreak: 'break-all',
    margin: '0 0 20px',
    display: 'block',
  },
  inlineCode: {
    fontFamily:
      'ui-monospace, SFMono-Regular, Menlo, Consolas, "Liberation Mono", monospace',
    backgroundColor: colors.codeBg,
    padding: '2px 6px',
    borderRadius: '4px',
    fontSize: '13px',
  },
  notice: {
    backgroundColor: colors.noticeBg,
    border: `1px solid ${colors.noticeBorder}`,
    borderRadius: '6px',
    padding: '12px 16px',
    margin: '0 0 20px',
    fontSize: '14px',
    color: colors.noticeText,
  },
  divider: {
    border: 'none',
    borderTop: `1px solid ${colors.border}`,
    margin: '24px 0',
  },
  footer: {
    padding: '20px 32px 24px',
    backgroundColor: '#fafbfc',
    borderTop: `1px solid ${colors.border}`,
  },
  footerText: {
    fontSize: '12px',
    color: colors.textFaint,
    margin: '0 0 6px',
    lineHeight: '1.5',
  },
} satisfies Record<string, CSSProperties>;
