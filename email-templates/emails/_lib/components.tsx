import type { ReactNode } from 'react';
import {
  Body,
  Container,
  Head,
  Heading,
  Hr,
  Html,
  Img,
  Link,
  Preview,
  Section,
  Text,
} from '@react-email/components';
import { styles } from './styles.ts';

export function Layout({
  heading,
  preview,
  children,
}: {
  heading: string;
  preview: string;
  children: ReactNode;
}) {
  return (
    <Html lang="en">
      <Head />
      <Preview>{preview}</Preview>
      <Body style={styles.body}>
        <Container style={styles.container}>
          <Header />
          <Section style={styles.content}>
            <Heading style={styles.heading}>{heading}</Heading>
            {children}
          </Section>
          <Footer />
        </Container>
      </Body>
    </Html>
  );
}

function Header() {
  return (
    <Section style={styles.header}>
      <Img
        src="https://wiki.openshock.org/branding/Logo128.png"
        height="32"
        alt="OpenShock"
        style={styles.brandLogo}
      />
    </Section>
  );
}

function Footer() {
  return (
    <Section style={styles.footer}>
      <Text style={styles.footerText}>
        OpenShock will never ask for your password, API token, or any other
        credentials by email. If a message appears to come from us and asks for
        these, do not respond.
      </Text>
      <Text style={styles.footerText}>
        Need help? Visit{' '}
        <Link href="https://openshock.app" style={{ color: 'inherit' }}>
          openshock.app
        </Link>
        .
      </Text>
      <Text style={styles.footerText}>
        © OpenShock. This is an automated message, please do not reply.
      </Text>
    </Section>
  );
}

export function Paragraph({ children }: { children: ReactNode }) {
  return <Text style={styles.text}>{children}</Text>;
}

export function MutedParagraph({ children }: { children: ReactNode }) {
  return <Text style={styles.muted}>{children}</Text>;
}

export function Greeting({ name }: { name: string }) {
  return <Paragraph>Hello {name},</Paragraph>;
}

export function Signoff() {
  return (
    <Paragraph>
      Thank you,
      <br />
      The OpenShock Team
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
    <Section style={styles.buttonSection}>
      <Link style={styles.button} href={href}>
        {children}
      </Link>
    </Section>
  );
}

export function RawLinkFallback({ href }: { href: string }) {
  return (
    <>
      <Text style={styles.rawLinkLabel}>
        If the button above doesn't work, copy and paste this link into your
        browser:
      </Text>
      <Link href={href} style={styles.rawLink}>
        {href}
      </Link>
    </>
  );
}

export function SecurityNotice({ children }: { children: ReactNode }) {
  return <Text style={styles.notice}>{children}</Text>;
}

export function Divider() {
  return <Hr style={styles.divider} />;
}

export function InlineCode({ children }: { children: ReactNode }) {
  return <span style={styles.inlineCode}>{children}</span>;
}
