import {
  CtaButton,
  Greeting,
  Layout,
  Paragraph,
  RawLinkFallback,
  SecurityNotice,
  Signoff,
} from './_lib/components.tsx';

export interface EmailVerificationProps {
  'To.Name': string;
  VerifyLink: string;
}

export const subject = 'Verify your new OpenShock email address';

export const sampleProps: EmailVerificationProps = {
  'To.Name': 'shockee',
  VerifyLink: 'https://openshock.app/verify?token=preview',
};

export function EmailVerification(props: EmailVerificationProps) {
  return (
    <Layout
      heading="Verify your email"
      preview="Confirm this email address to apply the change on your OpenShock account."
    >
      <Greeting name={props['To.Name']} />
      <Paragraph>
        Please confirm this is your email address by clicking the button below.
        The change will only take effect once this address has been verified.
      </Paragraph>
      <CtaButton href={props.VerifyLink}>Verify email</CtaButton>
      <RawLinkFallback href={props.VerifyLink} />
      <SecurityNotice>
        If you did not request this change, you can safely ignore this email.
        Your account email will not be updated.
      </SecurityNotice>
      <Signoff />
    </Layout>
  );
}

export default function Preview_EmailVerification() {
  return <EmailVerification {...sampleProps} />;
}
