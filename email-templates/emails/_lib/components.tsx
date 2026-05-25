import type { ReactNode } from 'react';
import {
  Body,
  Button,
  Container,
  Head,
  Heading,
  Html,
  Section,
  Text,
} from '@react-email/components';
import { styles } from './styles.ts';

export function Layout({
  heading,
  children,
}: {
  heading: string;
  children: ReactNode;
}) {
  return (
    <Html lang="en">
      <Head />
      <Body style={styles.body}>
        <Container style={styles.container}>
          <Heading style={styles.heading}>{heading}</Heading>
          {children}
        </Container>
      </Body>
    </Html>
  );
}

export function Paragraph({ children }: { children: ReactNode }) {
  return <Text style={styles.text}>{children}</Text>;
}

export function Greeting({ name }: { name: string }) {
  return <Paragraph>Hello {name},</Paragraph>;
}

export function Signoff() {
  return (
    <Paragraph>
      Thank you,
      <br />
      OpenShock Team
    </Paragraph>
  );
}

export function CtaButton({
  href,
  children,
}: {
  href: string;
  children: ReactNode;
}) {
  return (
    <Section>
      <Button style={styles.button} href={href}>
        {children}
      </Button>
    </Section>
  );
}

export function InlineCode({ children }: { children: ReactNode }) {
  return <span style={styles.inlineCode}>{children}</span>;
}
