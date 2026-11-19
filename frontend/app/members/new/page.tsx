import type { Metadata } from "next";
import { MemberForm } from "@/components/member-form";
import { NEW_MEMBER_VALUES } from "@/lib/member-form";
import { createMemberAction } from "../actions";

export const metadata: Metadata = { title: "Nouveau membre" };

export default function NewMemberPage() {
  return (
    <>
      <h1 className="mb-6 text-2xl font-semibold">Nouveau membre</h1>
      <MemberForm action={createMemberAction} initialValues={NEW_MEMBER_VALUES} cancelHref="/members" />
    </>
  );
}
