import { CircleX, DoorOpen, User } from "lucide-react";
import { Badge } from "@/components/badge";
import { ConfirmAction } from "@/components/confirm-action";
import type { Reservation } from "@/lib/api-client/types";
import { formatDay, formatPeriod } from "@/lib/format";
import { cancelReservationAction } from "../../reservations/actions";

// A booking in the planning. Only a confirmed one can be cancelled; a cancelled one (shown on request)
// keeps its text and gets a badge, so the status is never told by a colour alone.
export function ReservationCard({ reservation }: { reservation: Reservation }) {
  const period = formatPeriod(reservation.startsAt, reservation.endsAt);
  const isCancelled = reservation.status === "Cancelled";

  return (
    <article className="flex flex-col gap-3 rounded-lg border border-border bg-surface p-4 shadow-sm sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0">
        <p className="flex flex-wrap items-center gap-x-3 gap-y-1 font-semibold">
          {period}
          {isCancelled && <Badge tone="danger">Annulée</Badge>}
        </p>
        <p className="mt-1 break-words">{reservation.purpose}</p>
        <p className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <DoorOpen aria-hidden className="size-4 shrink-0" />
            {reservation.roomName}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <User aria-hidden className="size-4 shrink-0" />
            {reservation.memberName}
          </span>
        </p>
      </div>

      {!isCancelled && (
        <ConfirmAction
          triggerLabel="Annuler"
          triggerAriaLabel={`Annuler la réservation de ${reservation.memberName} en ${reservation.roomName}, ${formatDay(reservation.startsAt)}, ${period}`}
          title="Annuler cette réservation ?"
          description={`La réservation de ${reservation.memberName} en ${reservation.roomName} (${formatDay(reservation.startsAt)}, ${period}) sera annulée. Elle reste dans l'historique mais disparaît du planning.`}
          confirmLabel="Annuler la réservation"
          pendingLabel="Annulation…"
          dismissLabel="Garder la réservation"
          icon={<CircleX aria-hidden className="size-4" />}
          triggerVariant="secondary"
          confirmVariant="alert"
          action={cancelReservationAction.bind(null, reservation.id)}
        />
      )}
    </article>
  );
}
