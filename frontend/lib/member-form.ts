import { SUBSCRIPTION_TYPES, type Member, type MemberInput, type SubscriptionType } from "./api-client/types";
import { readText } from "./form-data";

// Names of the form fields, used to keep only the API messages that belong to this form.
export const MEMBER_FIELDS = ["name", "email", "subscription"] as const;

export type MemberFormValues = Record<(typeof MEMBER_FIELDS)[number], string>;

export const NEW_MEMBER_VALUES: MemberFormValues = { name: "", email: "", subscription: "Standard" };

export function readMemberFormValues(formData: FormData): MemberFormValues {
  return {
    name: readText(formData, "name"),
    email: readText(formData, "email"),
    subscription: readText(formData, "subscription"),
  };
}

export function toMemberInput(values: MemberFormValues): MemberInput {
  return { name: values.name, email: values.email, subscription: toSubscription(values.subscription) };
}

export function memberToFormValues(member: Member): MemberFormValues {
  return { name: member.name, email: member.email, subscription: member.subscription };
}

// An unknown value cannot come from our <select>: fall back to the default.
function toSubscription(text: string): SubscriptionType {
  return (SUBSCRIPTION_TYPES as readonly string[]).includes(text) ? (text as SubscriptionType) : "Standard";
}
