import type { CSSProperties } from 'react';

export const styles = {
  body: {
    fontFamily: 'Arial, sans-serif',
    lineHeight: '1.6',
    backgroundColor: '#f4f4f4',
    margin: '0',
    padding: '0',
  },
  container: {
    maxWidth: '600px',
    margin: '20px auto',
    padding: '20px',
    backgroundColor: '#fff',
    borderRadius: '5px',
    boxShadow: '0 0 10px rgba(0, 0, 0, 0.1)',
  },
  heading: {
    color: '#333',
  },
  text: {
    marginBottom: '20px',
  },
  button: {
    display: 'inline-block',
    padding: '10px 20px',
    backgroundColor: '#007bff',
    color: '#fff',
    textDecoration: 'none',
    borderRadius: '5px',
  },
  inlineCode: {
    fontFamily: 'monospace',
    backgroundColor: '#f0f0f0',
    padding: '2px 6px',
    borderRadius: '3px',
  },
} satisfies Record<string, CSSProperties>;
