import { Mail, Pencil } from "lucide-react";
import { Badge } from "@/components/badge";
import { ButtonLink } from "@/components/button";
import type { Member } from "@/lib/api-client/types";
import { SUBSCRIPTION_LABELS } from "@/lib/format";

// A member on the members page, with a link to edit them.
export function MemberCard({ member }: { member: Member }) {
  return (
    <article className="flex items-start justify-between gap-4 rounded-lg border border-border bg-surface p-4 shadow-sm">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
          <h2 className="break-words font-semibold">{member.name}</h2>
          <Badge tone={member.subscription === "Premium" ? "primary" : "muted"}>
            {SUBSCRIPTION_LABELS[member.subscription]}
          </Badge>
        </div>
        <p className="mt-2 flex items-center gap-2 text-sm text-muted-foreground">
          <Mail aria-hidden className="size-4 shrink-0" />
          <span className="break-all">{member.email}</span>
        </p>
      </div>

      <ButtonLink href={`/members/${member.id}/edit`} variant="secondary" aria-label={`Modifier ${member.name}`}>
        <Pencil aria-hidden className="size-4" />
      </ButtonLink>
    </article>
  );
}
