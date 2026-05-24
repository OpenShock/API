import {
  CtaButton,
  Greeting,
  Layout,
  Paragraph,
  Signoff,
} from './_lib/components.tsx';

export interface AccountActivationProps {
  'To.Name': string;
  ActivationLink: string;
}

export const subject = 'Hi! Activate your account';

export const sampleProps: AccountActivationProps = {
  'To.Name': 'shockee',
  ActivationLink: 'https://openshock.app/activate?token=preview',
};

export function AccountActivation(props: AccountActivationProps) {
  return (
    <Layout heading="Active your account!">
      <Greeting name={props['To.Name']} />
      <Paragraph>
        Thanks for signing up! Please verify your email address by clicking on
        the link below.
      </Paragraph>
      <CtaButton href={props.ActivationLink}>Activate Account</CtaButton>
      <Paragraph>
        If you did not sign up, you can safely ignore this email.
      </Paragraph>
      <Signoff />
    </Layout>
  );
}

export default function Preview_AccountActivation() {
  return <AccountActivation {...sampleProps} />;
}
