// Shared by NavLink (a client component) and by the fallback of MainNav (a server component), which is why
// it lives in its own file: a value exported from a "use client" file cannot be imported by server code.
export function navLinkClassName(isActive: boolean): string {
  return `inline-flex min-h-11 items-center rounded-md px-3 text-sm font-semibold ${
    isActive ? "bg-primary-subtle text-primary" : "text-muted-foreground hover:bg-background"
  }`;
}
