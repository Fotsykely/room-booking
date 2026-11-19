import { Building2, Pencil, Users, Wrench } from "lucide-react";
import { Badge } from "@/components/badge";
import { ButtonLink } from "@/components/button";
import type { Room } from "@/lib/api-client/types";
import { ROOM_STATUS_LABELS, formatCapacity, formatFloor, getRoomStatus, type RoomStatus } from "@/lib/format";

const STATUS_TONES = { free: "success", busy: "danger", inactive: "muted" } as const satisfies Record<
  RoomStatus,
  "success" | "danger" | "muted"
>;

// A room on the home page: what it offers, whether it is free right now, and a link to edit it.
export function RoomCard({ room }: { room: Room }) {
  const status = getRoomStatus(room);

  return (
    <article className="flex h-full flex-col gap-3 rounded-lg border border-border bg-surface p-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <h2 className="break-words font-semibold">{room.name}</h2>
        <Badge tone={STATUS_TONES[status]}>{ROOM_STATUS_LABELS[status]}</Badge>
      </div>

      <ul className="flex flex-col gap-1.5 text-sm text-muted-foreground">
        <li className="flex items-center gap-2">
          <Users aria-hidden className="size-4 shrink-0" />
          {formatCapacity(room.capacity)}
        </li>
        <li className="flex items-center gap-2">
          <Building2 aria-hidden className="size-4 shrink-0" />
          {formatFloor(room.floor)}
        </li>
        {room.equipment && (
          <li className="flex items-start gap-2">
            <Wrench aria-hidden className="mt-0.5 size-4 shrink-0" />
            <span className="break-words">{room.equipment}</span>
          </li>
        )}
      </ul>

      <div className="mt-auto pt-1">
        <ButtonLink href={`/rooms/${room.id}/edit`} variant="secondary">
          <Pencil aria-hidden className="size-4" />
          Modifier
          <span className="sr-only"> {room.name}</span>
        </ButtonLink>
      </div>
    </article>
  );
}
