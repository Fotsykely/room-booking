import type { Metadata } from "next";
import { Suspense } from "react";
import { FormSkeleton } from "@/components/form-skeleton";
import { RoomForm } from "@/components/room-form";
import { getRoom } from "@/lib/api-client/rooms";
import { orNotFound, parseRouteId } from "@/lib/route-helpers";
import { roomToFormValues } from "@/lib/room-form";
import { updateRoomAction } from "../../actions";

export const metadata: Metadata = { title: "Modifier la salle" };

type EditRoomPageProps = PageProps<"/rooms/[roomId]/edit">;

// The page itself renders instantly (static heading). Reading `params` and calling the API happen in a
// child inside its own <Suspense>: with Cache Components, a boundary above the shared layout (the root
// loading.tsx) does not give an instant fallback on a client navigation to this route.
export default function EditRoomPage(props: EditRoomPageProps) {
  return (
    <>
      <h1 className="mb-6 text-2xl font-semibold">Modifier la salle</h1>
      <Suspense fallback={<FormSkeleton fields={4} />}>
        <EditRoomForm params={props.params} />
      </Suspense>
    </>
  );
}

async function EditRoomForm({ params }: { params: EditRoomPageProps["params"] }) {
  const { roomId } = await params;
  const room = await orNotFound(getRoom(parseRouteId(roomId)));

  return (
    <RoomForm action={updateRoomAction.bind(null, room.id)} initialValues={roomToFormValues(room)} cancelHref="/" />
  );
}
