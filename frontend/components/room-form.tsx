"use client";

import { useActionState } from "react";
import { ButtonLink } from "@/components/button";
import { CheckboxField, FormError, InputField, TextAreaField } from "@/components/field";
import { SubmitButton } from "@/components/submit-button";
import { INITIAL_FORM_STATE, type FormState } from "@/lib/form-state";
import type { RoomFormValues } from "@/lib/room-form";

type RoomFormProps = {
  action: (previousState: FormState, formData: FormData) => Promise<FormState>;
  initialValues: RoomFormValues;
  cancelHref: string;
};

// Shared by the "new room" and "edit room" pages. The API validates; this form only displays
// the messages it sends back under the matching field (noValidate turns the browser's own checks off).
export function RoomForm({ action, initialValues, cancelHref }: RoomFormProps) {
  const [state, formAction] = useActionState(action, INITIAL_FORM_STATE);

  const values: Record<string, string> = state.status === "error" ? state.values : initialValues;
  const fieldErrors = state.status === "error" ? state.fieldErrors : {};

  return (
    <form action={formAction} noValidate className="flex max-w-xl flex-col gap-5">
      {state.status === "error" && state.message && <FormError>{state.message}</FormError>}

      <InputField label="Nom" name="name" defaultValue={values.name} errors={fieldErrors.name} />

      <div className="grid gap-5 sm:grid-cols-2">
        <InputField
          label="Capacité (personnes)"
          name="capacity"
          type="number"
          inputMode="numeric"
          min={1}
          defaultValue={values.capacity}
          errors={fieldErrors.capacity}
        />
        <InputField
          label="Étage"
          name="floor"
          type="number"
          inputMode="numeric"
          hint="0 pour le rez-de-chaussée."
          defaultValue={values.floor}
          errors={fieldErrors.floor}
        />
      </div>

      <TextAreaField
        label="Équipements"
        name="equipment"
        optional
        rows={3}
        defaultValue={values.equipment}
        errors={fieldErrors.equipment}
      />

      <CheckboxField
        label="Salle active"
        name="isActive"
        hint="Décochée, la salle ne peut plus être réservée, mais son historique est conservé."
        defaultChecked={values.isActive === "on"}
        errors={fieldErrors.isActive}
      />

      <div className="flex flex-wrap gap-3">
        <SubmitButton>Enregistrer</SubmitButton>
        <ButtonLink href={cancelHref} variant="secondary">
          Annuler
        </ButtonLink>
      </div>
    </form>
  );
}
