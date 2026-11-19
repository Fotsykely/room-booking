import Link from "next/link";
import { Suspense } from "react";
import { NavLink } from "./nav-link";
import { navLinkClassName } from "./nav-link-style";

const NAV_ITEMS = [
  { href: "/", label: "Salles", alsoActiveFor: ["/rooms"] },
  { href: "/members", label: "Membres", alsoActiveFor: [] },
];

export function MainNav() {
  return (
    <nav aria-label="Navigation principale" className="flex flex-wrap gap-1">
      {/* The current path is only known at request time, so the active state streams in. Until then the same
          links are shown without it, which keeps the header from jumping. */}
      <Suspense
        fallback={NAV_ITEMS.map((item) => (
          <Link key={item.href} href={item.href} className={navLinkClassName(false)}>
            {item.label}
          </Link>
        ))}
      >
        {NAV_ITEMS.map((item) => (
          <NavLink key={item.href} href={item.href} alsoActiveFor={item.alsoActiveFor}>
            {item.label}
          </NavLink>
        ))}
      </Suspense>
    </nav>
  );
}
