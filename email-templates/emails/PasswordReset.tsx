import {
  CtaButton,
  Greeting,
  Layout,
  Paragraph,
  RawLinkFallback,
  SecurityNotice,
  Signoff,
} from './_lib/components.tsx';

export interface PasswordResetProps {
  'To.Name': string;
  ResetLink: string;
}

export const subject = 'Reset your OpenShock password';

export const sampleProps: PasswordResetProps = {
  'To.Name': 'shockee',
  ResetLink: 'https://openshock.app/reset?token=preview',
};

export function PasswordReset(props: PasswordResetProps) {
  return (
    <Layout
      heading="Password reset"
      preview="Reset the password for your OpenShock account."
    >
      <Greeting name={props['To.Name']} />
      <Paragraph>
        We received a request to reset the password for your OpenShock account.
        Click the button below to choose a new one.
      </Paragraph>
      <CtaButton href={props.ResetLink}>Reset password</CtaButton>
      <RawLinkFallback href={props.ResetLink} />
      <SecurityNotice>
        This link is single-use and will expire shortly. If you did not request
        a password reset, you can safely ignore this email. Your password will
        remain unchanged. If you receive these messages repeatedly, sign in and
        review your account's recent activity.
      </SecurityNotice>
      <Signoff />
    </Layout>
  );
}

export default function Preview_PasswordReset() {
  return <PasswordReset {...sampleProps} />;
}
