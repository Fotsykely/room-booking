import type { Metadata } from "next";
import { Suspense } from "react";
import { FormSkeleton } from "@/components/form-skeleton";
import { MemberForm } from "@/components/member-form";
import { getMember } from "@/lib/api-client/members";
import { memberToFormValues } from "@/lib/member-form";
import { orNotFound, parseRouteId } from "@/lib/route-helpers";
import { updateMemberAction } from "../../actions";

export const metadata: Metadata = { title: "Modifier le membre" };

type EditMemberPageProps = PageProps<"/members/[memberId]/edit">;

// Same structure as the room edit page: static heading, then the data read inside its own <Suspense>.
export default function EditMemberPage(props: EditMemberPageProps) {
  return (
    <>
      <h1 className="mb-6 text-2xl font-semibold">Modifier le membre</h1>
      <Suspense fallback={<FormSkeleton fields={3} />}>
        <EditMemberForm params={props.params} />
      </Suspense>
    </>
  );
}

async function EditMemberForm({ params }: { params: EditMemberPageProps["params"] }) {
  const { memberId } = await params;
  const member = await orNotFound(getMember(parseRouteId(memberId)));

  return (
    <MemberForm
      action={updateMemberAction.bind(null, member.id)}
      initialValues={memberToFormValues(member)}
      cancelHref="/members"
    />
  );
}
