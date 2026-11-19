import type { Metadata } from "next";
import { Plus } from "lucide-react";
import { Button, ButtonLink } from "@/components/button";
import { CheckboxField, InputField, SelectField } from "@/components/field";
import { listReservations } from "@/lib/api-client/reservations";
import { listRooms } from "@/lib/api-client/rooms";
import { formatDay } from "@/lib/format";
import { groupByDay, parsePlanningFilters } from "@/lib/planning";
import { ReservationCard } from "./_components/reservation-card";

export const metadata: Metadata = { title: "Planning" };

type PlanningPageProps = PageProps<"/planning">;

// The filters live in the address (/planning?roomId=2&date=2026-11-20), so a filtered planning can be
// bookmarked or shared. The filter form is a plain GET form: it works without JavaScript.
export default async function PlanningPage(props: PlanningPageProps) {
  const filters = parsePlanningFilters(await props.searchParams);
  const [rooms, reservations] = await Promise.all([listRooms(), listReservations(filters)]);

  const hasFilters = filters.roomId !== undefined || filters.date !== undefined || filters.includeCancelled;
  const newReservationHref =
    filters.roomId === undefined ? "/reservations/new" : `/reservations/new?roomId=${filters.roomId}`;

  return (
    <>
      <header className="mb-6 flex items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">Planning</h1>
        <ButtonLink href={newReservationHref}>
          <Plus aria-hidden className="size-4" />
          Réserver
        </ButtonLink>
      </header>

      <form
        method="get"
        action="/planning"
        className="mb-6 grid gap-4 rounded-lg border border-border bg-surface p-4 shadow-sm sm:grid-cols-2 lg:grid-cols-[1fr_1fr_auto_auto] lg:items-end"
      >
        <SelectField label="Salle" name="roomId" defaultValue={filters.roomId?.toString() ?? ""}>
          <option value="">Toutes les salles</option>
          {rooms.map((room) => (
            <option key={room.id} value={room.id}>
              {room.isActive ? room.name : `${room.name} (inactive)`}
            </option>
          ))}
        </SelectField>
        <InputField label="Date" name="date" type="date" defaultValue={filters.date ?? ""} />
        <CheckboxField label="Afficher les annulées" name="includeCancelled" value="true" defaultChecked={filters.includeCancelled} />
        <div className="flex flex-wrap gap-3">
          <Button type="submit">Filtrer</Button>
          {hasFilters && (
            <ButtonLink href="/planning" variant="secondary">
              Réinitialiser
            </ButtonLink>
          )}
        </div>
      </form>

      {reservations.length === 0 ? (
        <p className="rounded-lg border border-dashed border-border-input p-6 text-center text-muted-foreground">
          {hasFilters
            ? "Aucune réservation ne correspond à ces filtres."
            : "Aucune réservation pour le moment. Créez la première avec le bouton « Réserver »."}
        </p>
      ) : (
        <>
          <p className="mb-4 text-sm text-muted-foreground">
            {reservations.length === 1 ? "1 réservation" : `${reservations.length} réservations`}
          </p>
          <div className="flex flex-col gap-6">
            {groupByDay(reservations).map((group) => (
              <section key={group.day} aria-labelledby={`day-${group.day}`}>
                <h2 id={`day-${group.day}`} className="mb-3 text-lg font-semibold first-letter:uppercase">
                  {formatDay(group.day)}
                </h2>
                <ul className="grid gap-3">
                  {group.reservations.map((reservation) => (
                    <li key={reservation.id}>
                      <ReservationCard reservation={reservation} />
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
        </>
      )}
    </>
  );
}
