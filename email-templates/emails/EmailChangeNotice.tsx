import {
  Greeting,
  InlineCode,
  Layout,
  Paragraph,
  SecurityNotice,
  Signoff,
} from './_lib/components.tsx';

export interface EmailChangeNoticeProps {
  'To.Name': string;
  NewEmail: string;
}

export const subject = 'Your OpenShock email is being changed';

export const sampleProps: EmailChangeNoticeProps = {
  'To.Name': 'shockee',
  NewEmail: 'new-address@example.com',
};

export function EmailChangeNotice(props: EmailChangeNoticeProps) {
  return (
    <Layout
      heading="Email change requested"
      preview="A change to your OpenShock account email was requested."
    >
      <Greeting name={props['To.Name']} />
      <Paragraph>
        Someone requested that the email address on your OpenShock account be
        changed to <InlineCode>{props.NewEmail}</InlineCode>.
      </Paragraph>
      <Paragraph>
        The change is <strong>not yet applied</strong>. It will only take effect
        once the new address is verified via the link sent to it.
      </Paragraph>
      <Paragraph>If this was you, no further action is needed.</Paragraph>
      <SecurityNotice>
        If this was <strong>not you</strong>, sign in immediately and change
        your password. Your account may be compromised. You should also review
        active sessions and API tokens.
      </SecurityNotice>
      <Signoff />
    </Layout>
  );
}

export default function Preview_EmailChangeNotice() {
  return <EmailChangeNotice {...sampleProps} />;
}
