import {
  CtaButton,
  Greeting,
  Layout,
  Paragraph,
  Signoff,
} from './_lib/components.tsx';

export interface PasswordResetProps {
  'To.Name': string;
  ResetLink: string;
}

export const subject = 'Password reset request';

export const sampleProps: PasswordResetProps = {
  'To.Name': 'shockee',
  ResetLink: 'https://openshock.app/reset?token=preview',
};

export function PasswordReset(props: PasswordResetProps) {
  return (
    <Layout heading="Password Reset">
      <Greeting name={props['To.Name']} />
      <Paragraph>
        We have received a request to reset the password for your account.
        Click the button below to reset your password:
      </Paragraph>
      <CtaButton href={props.ResetLink}>Reset Password</CtaButton>
      <Paragraph>
        If you did not request this change, you can safely ignore this email.
      </Paragraph>
      <Signoff />
    </Layout>
  );
}

export default function Preview_PasswordReset() {
  return <PasswordReset {...sampleProps} />;
}
