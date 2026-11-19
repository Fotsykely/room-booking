"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { navLinkClassName } from "./nav-link-style";

type NavLinkProps = {
  href: string;
  // Other path prefixes that belong to this section (the room pages live under /rooms, the list is at /).
  alsoActiveFor?: string[];
  children: ReactNode;
};

// A link of the main navigation. The current section is told with aria-current, not by colour alone.
// usePathname() is only known at request time: MainNav renders this component inside a <Suspense>.
export function NavLink({ href, alsoActiveFor = [], children }: NavLinkProps) {
  const pathname = usePathname();
  const isActive =
    pathname === href ||
    (href !== "/" && pathname.startsWith(`${href}/`)) ||
    alsoActiveFor.some((prefix) => pathname.startsWith(prefix));

  return (
    <Link href={href} aria-current={isActive ? "page" : undefined} className={navLinkClassName(isActive)}>
      {children}
    </Link>
  );
}
