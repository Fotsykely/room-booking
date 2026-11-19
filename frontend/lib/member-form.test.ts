import { describe, expect, it } from "vitest";
import { memberToFormValues, readMemberFormValues, toMemberInput } from "./member-form";

function formDataOf(fields: Record<string, string>): FormData {
  const formData = new FormData();
  for (const [name, value] of Object.entries(fields)) formData.set(name, value);
  return formData;
}

describe("member form", () => {
  it("reads the three fields", () => {
    const values = readMemberFormValues(
      formDataOf({ name: "Hery Rakoto", email: "hery@example.com", subscription: "Premium" }),
    );

    expect(toMemberInput(values)).toEqual({ name: "Hery Rakoto", email: "hery@example.com", subscription: "Premium" });
  });

  it("falls back to Standard for a subscription that is not in the list", () => {
    const input = toMemberInput({ name: "A", email: "a@example.com", subscription: "Gold" });

    expect(input.subscription).toBe("Standard");
  });

  it("keeps a missing field empty so that the API can refuse it", () => {
    const values = readMemberFormValues(formDataOf({ subscription: "Standard" }));

    expect(values).toEqual({ name: "", email: "", subscription: "Standard" });
  });

  it("fills the edit form from a member", () => {
    const values = memberToFormValues({ id: 2, name: "Tiana Rabe", email: "tiana@example.com", subscription: "Premium" });

    expect(values).toEqual({ name: "Tiana Rabe", email: "tiana@example.com", subscription: "Premium" });
  });
});
