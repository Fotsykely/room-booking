import type { Metadata } from "next";
import { RoomForm } from "@/components/room-form";
import { NEW_ROOM_VALUES } from "@/lib/room-form";
import { createRoomAction } from "../actions";

export const metadata: Metadata = { title: "Nouvelle salle" };

export default function NewRoomPage() {
  return (
    <>
      <h1 className="mb-6 text-2xl font-semibold">Nouvelle salle</h1>
      <RoomForm action={createRoomAction} initialValues={NEW_ROOM_VALUES} cancelHref="/" />
    </>
  );
}
