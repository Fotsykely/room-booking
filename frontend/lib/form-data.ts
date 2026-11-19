// FormData values are a string or a File; our forms only have text fields.
export function readText(formData: FormData, field: string): string {
  const value = formData.get(field);
  return typeof value === "string" ? value : "";
}

// A number input gives text. A blank field counts as 0, which the API refuses with a clear message
// where 0 is not allowed (a capacity) and which is a valid value elsewhere (the ground floor).
// Text that is not a number gives NaN, sent as null: the API answers with a generic 400.
export function parseInteger(text: string): number {
  return text.trim() === "" ? 0 : Number(text);
}
