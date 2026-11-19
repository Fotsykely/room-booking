"use client";

import { useId, useRef, type ComponentProps, type ReactNode } from "react";
import { Button } from "@/components/button";
import { SubmitButton } from "@/components/submit-button";

type ButtonVariant = ComponentProps<typeof Button>["variant"];

type ConfirmActionProps = {
  triggerLabel: string;
  title: string;
  description: string;
  confirmLabel: string;
  // Shown on the confirm button while the action runs ("Annulation…").
  pendingLabel: string;
  // Text of the button that closes the dialog without acting. It must not be confused with the action itself:
  // for "cancel a reservation", "Annuler" would be ambiguous, so the caller says "Garder la réservation".
  dismissLabel?: string;
  icon: ReactNode;
  // Server Action bound to the id of the thing to act on.
  action: (formData: FormData) => Promise<void>;
  // The button that opens the dialog, and the one that confirms.
  triggerVariant?: ButtonVariant;
  confirmVariant?: ButtonVariant;
  // Icon-only trigger for compact rows; the label stays available to screen readers.
  iconOnly?: boolean;
};

// A button that asks for confirmation in a native modal <dialog> before running a Server Action.
export function ConfirmAction({
  triggerLabel,
  title,
  description,
  confirmLabel,
  pendingLabel,
  dismissLabel = "Annuler",
  icon,
  action,
  triggerVariant = "danger",
  confirmVariant = "destructive",
  iconOnly = false,
}: ConfirmActionProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  return (
    <>
      <Button
        type="button"
        variant={triggerVariant}
        aria-label={iconOnly ? triggerLabel : undefined}
        onClick={() => dialogRef.current?.showModal()}
      >
        {icon}
        {!iconOnly && triggerLabel}
      </Button>

      <dialog
        ref={dialogRef}
        aria-labelledby={titleId}
        className="m-auto w-[calc(100%-2rem)] max-w-md rounded-lg border border-border bg-surface p-6 text-foreground shadow-sm backdrop:bg-foreground/40"
      >
        <form action={action} className="flex flex-col gap-4">
          <h2 id={titleId} className="text-lg font-semibold">
            {title}
          </h2>
          <p className="text-muted-foreground">{description}</p>
          <div className="flex flex-wrap gap-3">
            <SubmitButton variant={confirmVariant} pendingLabel={pendingLabel}>
              {confirmLabel}
            </SubmitButton>
            <Button type="button" variant="secondary" onClick={() => dialogRef.current?.close()}>
              {dismissLabel}
            </Button>
          </div>
        </form>
      </dialog>
    </>
  );
}
