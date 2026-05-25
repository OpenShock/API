import {
  CtaButton,
  Greeting,
  Layout,
  Paragraph,
  Signoff,
} from './_lib/components.tsx';

export interface EmailVerificationProps {
  'To.Name': string;
  VerifyLink: string;
}

export const subject = 'Hi! Verify your Email!';

export const sampleProps: EmailVerificationProps = {
  'To.Name': 'shockee',
  VerifyLink: 'https://openshock.app/verify?token=preview',
};

export function EmailVerification(props: EmailVerificationProps) {
  return (
    <Layout heading="Email verification">
      <Greeting name={props['To.Name']} />
      <Paragraph>
        Thanks for signing up! Please verify your email address by clicking on
        the link below.
      </Paragraph>
      <CtaButton href={props.VerifyLink}>Verify Email</CtaButton>
      <Paragraph>
        If you did not sign up, you can safely ignore this email.
      </Paragraph>
      <Signoff />
    </Layout>
  );
}

export default function Preview_EmailVerification() {
  return <EmailVerification {...sampleProps} />;
}
