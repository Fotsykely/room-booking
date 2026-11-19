import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { FormSkeleton } from "@/components/form-skeleton";
import { ReservationForm } from "@/components/reservation-form";
import { listMembers } from "@/lib/api-client/members";
import { listRooms } from "@/lib/api-client/rooms";
import {
  NEW_RESERVATION_VALUES,
  preselectRoom,
  toMemberOptions,
  toRoomOptions,
} from "@/lib/reservation-form";
import { createReservationAction } from "../actions";

export const metadata: Metadata = { title: "Nouvelle réservation" };

type NewReservationPageProps = PageProps<"/reservations/new">;

// Same structure as the edit pages: static heading, then the data (and the address) read inside <Suspense>.
export default function NewReservationPage(props: NewReservationPageProps) {
  return (
    <>
      <h1 className="mb-6 text-2xl font-semibold">Nouvelle réservation</h1>
      <Suspense fallback={<FormSkeleton fields={5} />}>
        <NewReservationForm searchParams={props.searchParams} />
      </Suspense>
    </>
  );
}

async function NewReservationForm({ searchParams }: { searchParams: NewReservationPageProps["searchParams"] }) {
  const [{ roomId }, rooms, members] = await Promise.all([searchParams, listRooms(), listMembers()]);
  const roomOptions = toRoomOptions(rooms);
  const memberOptions = toMemberOptions(members);

  // Nothing to choose from: say what to create first instead of showing a form that cannot succeed.
  if (roomOptions.length === 0 || memberOptions.length === 0) {
    return (
      <p className="max-w-xl rounded-lg border border-dashed border-border-input p-6 text-muted-foreground">
        Pour réserver, il faut au moins une salle active et un membre.{" "}
        <Link href="/rooms/new" className="font-semibold text-primary underline">
          Créer une salle
        </Link>{" "}
        ·{" "}
        <Link href="/members/new" className="font-semibold text-primary underline">
          Créer un membre
        </Link>
      </p>
    );
  }

  return (
    <ReservationForm
      action={createReservationAction}
      initialValues={{ ...NEW_RESERVATION_VALUES, roomId: preselectRoom(roomId, roomOptions) }}
      rooms={roomOptions}
      members={memberOptions}
      cancelHref="/planning"
    />
  );
}
