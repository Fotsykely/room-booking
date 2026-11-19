import { revalidatePath } from "next/cache";

// Every change can touch several pages: a room shows on the room list and, later, on the planning and in the
// booking form. Refreshing everything is simpler than tracking each path.
export function revalidateAllPages(): void {
  revalidatePath("/", "layout");
}
