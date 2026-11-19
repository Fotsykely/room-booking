import type { ReactNode } from "react";

type BadgeTone = "success" | "danger" | "muted" | "primary";

const TONE_CLASSES: Record<BadgeTone, string> = {
  success: "bg-success-subtle text-success",
  danger: "bg-danger-subtle text-danger",
  muted: "bg-muted-subtle text-muted-foreground",
  primary: "bg-primary-subtle text-primary",
};

// A short status label. The meaning is always in the text, the colour only reinforces it.
export function Badge({ tone, children }: { tone: BadgeTone; children: ReactNode }) {
  return (
    <span
      className={`inline-flex shrink-0 items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${TONE_CLASSES[tone]}`}
    >
      {children}
    </span>
  );
}
