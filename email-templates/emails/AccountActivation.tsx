import {
  CtaButton,
  Greeting,
  Layout,
  Paragraph,
  RawLinkFallback,
  SecurityNotice,
  Signoff,
} from './_lib/components.tsx';

export interface AccountActivationProps {
  'To.Name': string;
  ActivationLink: string;
}

export const subject = 'Activate your OpenShock account';

export const sampleProps: AccountActivationProps = {
  'To.Name': 'shockee',
  ActivationLink: 'https://openshock.app/activate?token=preview',
};

export function AccountActivation(props: AccountActivationProps) {
  return (
    <Layout
      heading="Activate your account"
      preview="One last step: activate your new OpenShock account."
    >
      <Greeting name={props['To.Name']} />
      <Paragraph>
        Thanks for signing up! Confirm this is your email address to finish
        setting up your OpenShock account.
      </Paragraph>
      <CtaButton href={props.ActivationLink}>Activate account</CtaButton>
      <RawLinkFallback href={props.ActivationLink} />
      <SecurityNotice>
        If you did not sign up for OpenShock, you can safely ignore this email.
        No account will be created without confirmation.
      </SecurityNotice>
      <Signoff />
    </Layout>
  );
}

export default function Preview_AccountActivation() {
  return <AccountActivation {...sampleProps} />;
}
