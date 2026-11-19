"use client";

import { useActionState } from "react";
import { ButtonLink } from "@/components/button";
import { FormError, InputField, SelectField } from "@/components/field";
import { SubmitButton } from "@/components/submit-button";
import { INITIAL_FORM_STATE, type FormState } from "@/lib/form-state";
import type { ReservationFormValues, SelectOption } from "@/lib/reservation-form";

type ReservationFormProps = {
  action: (previousState: FormState, formData: FormData) => Promise<FormState>;
  initialValues: ReservationFormValues;
  rooms: SelectOption[];
  members: SelectOption[];
  cancelHref: string;
};

// The booking form. The API checks every rule; this form only displays the messages it sends back under the
// matching field (noValidate turns the browser's own checks off) and keeps what the user typed.
export function ReservationForm({ action, initialValues, rooms, members, cancelHref }: ReservationFormProps) {
  const [state, formAction] = useActionState(action, INITIAL_FORM_STATE);

  const values: Record<string, string> = state.status === "error" ? state.values : initialValues;
  const fieldErrors = state.status === "error" ? state.fieldErrors : {};

  return (
    <form action={formAction} noValidate className="flex max-w-xl flex-col gap-5">
      {state.status === "error" && state.message && <FormError>{state.message}</FormError>}

      <SelectField label="Membre" name="memberId" defaultValue={values.memberId} errors={fieldErrors.memberId}>
        <option value="">Choisir un membre…</option>
        {members.map((member) => (
          <option key={member.value} value={member.value}>
            {member.label}
          </option>
        ))}
      </SelectField>

      <SelectField label="Salle" name="roomId" defaultValue={values.roomId} errors={fieldErrors.roomId}>
        <option value="">Choisir une salle…</option>
        {rooms.map((room) => (
          <option key={room.value} value={room.value}>
            {room.label}
          </option>
        ))}
      </SelectField>

      <div className="grid gap-5 sm:grid-cols-2">
        <InputField
          label="Début"
          name="startsAt"
          type="datetime-local"
          hint="Heure locale de l'espace."
          defaultValue={values.startsAt}
          errors={fieldErrors.startsAt}
        />
        <InputField
          label="Fin"
          name="endsAt"
          type="datetime-local"
          hint="4 heures au maximum."
          defaultValue={values.endsAt}
          errors={fieldErrors.endsAt}
        />
      </div>

      <InputField label="Objet" name="purpose" defaultValue={values.purpose} errors={fieldErrors.purpose} />

      <div className="flex flex-wrap gap-3">
        <SubmitButton pendingLabel="Réservation…">Réserver</SubmitButton>
        <ButtonLink href={cancelHref} variant="secondary">
          Annuler
        </ButtonLink>
      </div>
    </form>
  );
}
