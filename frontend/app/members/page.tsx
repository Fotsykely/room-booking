import type { Metadata } from "next";
import { Plus } from "lucide-react";
import { ButtonLink } from "@/components/button";
import { listMembers } from "@/lib/api-client/members";
import { MemberCard } from "./_components/member-card";

export const metadata: Metadata = { title: "Membres" };

export default async function MembersPage() {
  const members = await listMembers();

  return (
    <>
      <header className="mb-6 flex items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">Membres</h1>
        <ButtonLink href="/members/new">
          <Plus aria-hidden className="size-4" />
          Nouveau membre
        </ButtonLink>
      </header>

      {members.length === 0 ? (
        <p className="rounded-lg border border-dashed border-border-input p-6 text-center text-muted-foreground">
          Aucun membre pour le moment. Créez le premier avec le bouton « Nouveau membre ».
        </p>
      ) : (
        <ul className="grid gap-3 md:grid-cols-2">
          {members.map((member) => (
            <li key={member.id}>
              <MemberCard member={member} />
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
