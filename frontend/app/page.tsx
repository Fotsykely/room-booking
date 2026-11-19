import { Plus } from "lucide-react";
import { ButtonLink } from "@/components/button";
import { listRooms } from "@/lib/api-client/rooms";
import { RoomCard } from "./_components/room-card";

export default async function HomePage() {
  const rooms = await listRooms();

  return (
    <>
      <header className="mb-6 flex items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">Salles</h1>
        <ButtonLink href="/rooms/new">
          <Plus aria-hidden className="size-4" />
          Nouvelle salle
        </ButtonLink>
      </header>

      {rooms.length === 0 ? (
        <p className="rounded-lg border border-dashed border-border-input p-6 text-center text-muted-foreground">
          Aucune salle pour le moment. Créez la première avec le bouton « Nouvelle salle ».
        </p>
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {rooms.map((room) => (
            <li key={room.id}>
              <RoomCard room={room} />
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
