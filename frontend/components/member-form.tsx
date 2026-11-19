"use client";

import { useActionState } from "react";
import { ButtonLink } from "@/components/button";
import { FormError, InputField, SelectField } from "@/components/field";
import { SubmitButton } from "@/components/submit-button";
import { SUBSCRIPTION_TYPES } from "@/lib/api-client/types";
import { SUBSCRIPTION_LABELS } from "@/lib/format";
import { INITIAL_FORM_STATE, type FormState } from "@/lib/form-state";
import type { MemberFormValues } from "@/lib/member-form";

type MemberFormProps = {
  action: (previousState: FormState, formData: FormData) => Promise<FormState>;
  initialValues: MemberFormValues;
  cancelHref: string;
};

// Shared by the "new member" and "edit member" pages (same principle as RoomForm).
export function MemberForm({ action, initialValues, cancelHref }: MemberFormProps) {
  const [state, formAction] = useActionState(action, INITIAL_FORM_STATE);

  const values: Record<string, string> = state.status === "error" ? state.values : initialValues;
  const fieldErrors = state.status === "error" ? state.fieldErrors : {};

  return (
    <form action={formAction} noValidate className="flex max-w-xl flex-col gap-5">
      {state.status === "error" && state.message && <FormError>{state.message}</FormError>}

      <InputField label="Nom" name="name" defaultValue={values.name} errors={fieldErrors.name} />
      <InputField
        label="Adresse e-mail"
        name="email"
        type="email"
        inputMode="email"
        autoComplete="off"
        defaultValue={values.email}
        errors={fieldErrors.email}
      />
      <SelectField
        label="Abonnement"
        name="subscription"
        hint="Un membre Standard a au plus 3 réservations à venir. Premium : sans limite."
        defaultValue={values.subscription}
        errors={fieldErrors.subscription}
      >
        {SUBSCRIPTION_TYPES.map((subscription) => (
          <option key={subscription} value={subscription}>
            {SUBSCRIPTION_LABELS[subscription]}
          </option>
        ))}
      </SelectField>

      <div className="flex flex-wrap gap-3">
        <SubmitButton>Enregistrer</SubmitButton>
        <ButtonLink href={cancelHref} variant="secondary">
          Annuler
        </ButtonLink>
      </div>
    </form>
  );
}
