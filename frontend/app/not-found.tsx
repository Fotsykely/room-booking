import { ButtonLink } from "@/components/button";

// Shown for unknown URLs and whenever a page calls notFound() (unknown room or member id).
export default function NotFound() {
  return (
    <div className="flex flex-col items-start gap-4">
      <h1 className="text-2xl font-semibold">Page introuvable</h1>
      <p className="text-muted-foreground">Cette page n&apos;existe pas (ou la salle ou le membre est introuvable).</p>
      <ButtonLink href="/">Retour à l&apos;accueil</ButtonLink>
    </div>
  );
}
