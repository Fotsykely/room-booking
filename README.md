# Réservation de salles

Un espace de coworking gérait ses salles de réunion par échanges d'e-mails, ce qui provoquait des doubles réservations. Ce site permet aux membres de **réserver une salle** et au gérant de **voir le planning**, sans conflit possible.

**Stack** : Next.js 16 (front) · ASP.NET Core / .NET 10 (API) · Entity Framework Core + MySQL 8.4 (fournisseur Pomelo).

## Ce que fait le site

| Page | Rôle |
|---|---|
| **Salles** (`/`) | Liste des salles avec capacité, étage, équipements et un badge **Libre / Occupée / Inactive** calculé à l'instant. Création et modification. |
| **Réserver** (`/reservations/new`) | Formulaire : membre, salle, début, fin, objet. Chaque règle violée s'affiche sous le champ concerné. |
| **Planning** (`/planning`) | Réservations confirmées, par jour, triées par début. Filtres par salle et par date, option « Afficher les annulées », annulation avec confirmation. |
| **Membres** (`/members`) | Liste, création et modification (nom, e-mail, abonnement Standard ou Premium). |

## Lancer le projet

Prérequis : **.NET SDK 10** (voir `global.json`), **Node.js 20.9 ou plus**, **Docker**.

Trois terminaux.

**1. Base de données** (à la racine) :

```bash
cp .env.example .env        # ajustez le mot de passe et le port si besoin
docker compose up -d        # MySQL 8.4 sur le port 3307
```

**2. API** :

```bash
cd backend/Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3307;Database=app;User=app;Password=change-me"
dotnet run                  # http://localhost:5080
```

Le port et le mot de passe doivent correspondre à votre `.env`. La chaîne de connexion n'est **jamais dans le code** : elle vient des *user secrets* en développement, ou de la variable d'environnement `ConnectionStrings__Default` ailleurs.

Au démarrage, l'API applique les migrations, puis insère les données de démonstration si la base ne contient aucune salle. Documentation interactive de l'API (en développement) : <http://localhost:5080/scalar>.

**3. Front** :

```bash
cd frontend
cp .env.example .env.local  # adresse de l'API : http://localhost:5080
npm install
npm run dev                 # http://localhost:3000
```

Sans `.env.local`, la page affiche « Une erreur est survenue » et le terminal du front indique `API_BASE_URL is not set`. Redémarrez `npm run dev` après avoir créé le fichier : Next ne relit les fichiers `.env*` qu'au démarrage.

### Données de démonstration

4 salles, 5 membres et 10 réservations, dont des réservations **passées**, une **en cours**, des **à venir** et une **annulée**. Les dates sont calculées par rapport au premier lancement, donc la démo reste cohérente quel que soit le jour.

- **Salle Vanille** est inactive (elle garde une réservation passée).
- **Salle Zébu** est occupée pendant les 2 heures qui suivent le premier lancement.
- **Mialy Randria** (abonnement Standard) a déjà ses 3 réservations à venir : une 4ᵉ est refusée. Faly et Tiana (Premium) n'ont pas de limite.

Pour repartir de zéro (par exemple pour revoir la salle occupée) : `docker compose down -v && docker compose up -d`, puis relancer l'API.

## Modèle de données

```
Room         Id · Name (unique) · Capacity (> 0) · Floor (0 = rez-de-chaussée) · Equipment (texte libre) · IsActive
Member       Id · Name · Email (unique) · Subscription (Standard | Premium)
Reservation  Id · RoomId → Room · MemberId → Member · StartsAt · EndsAt · Purpose · Status (Confirmed | Cancelled) · CreatedAt
```

- Rien n'est jamais supprimé : les clés étrangères sont en `RESTRICT`, il n'y a pas de route `DELETE`, et une réservation annulée change seulement de statut.
- Contraintes de dernier recours dans la base : `Capacity > 0` et `EndsAt > StartsAt`.
- Les statuts sont stockés en texte (`Confirmed`, `Premium`…), lisibles directement en SQL.

**Index** (schéma géré par migrations, une requête par index) :

| Index | Sert à |
|---|---|
| `Reservations (RoomId, Status, StartsAt)` | contrôle de chevauchement, planning filtré par salle, `isAvailableNow` |
| `Reservations (Status, StartsAt)` | planning sans filtre de salle, trié par début et filtré par jour |
| `Reservations (MemberId, Status, StartsAt)` | limite des réservations à venir d'un membre |
| `Rooms (Name)` unique · `Members (Email)` unique | unicité (sans tenir compte de la casse ni des accents) |

## Règles métier implémentées

Toutes sont dans `backend/Api/Services/ReservationService.cs` (méthode `BookAsync`).

| Règle | Où |
|---|---|
| La fin est après le début, et une réservation dure **4 h au maximum** | `CheckPeriod` |
| Une réservation **ne commence pas dans le passé** (la minute en cours est acceptée) | `CheckPeriod` |
| **Pas de chevauchement** sur une même salle. Une réservation qui finit à 14 h ne chevauche pas celle qui commence à 14 h (intervalles semi-ouverts) | `CheckRoomIsFreeAsync` |
| Un membre **Standard** a au plus **3** réservations confirmées à venir ; un **Premium** n'a pas de limite | `CheckUpcomingLimitAsync` |
| Une **salle désactivée** ne peut plus être réservée mais **garde son historique** | `BookAsync` |
| **Annuler** ne supprime pas : le statut passe à `Cancelled` et la réservation disparaît du planning par défaut | `ReservationsController.Cancel` |
| Une règle violée donne un **message clair, sous le champ concerné** | réponse 400 par champ (`RoomId`, `MemberId`, `StartsAt`, `EndsAt`) |

**Double réservation et requêtes simultanées.** Sans précaution, deux personnes qui réservent le même créneau à la même seconde passeraient toutes les deux la vérification « la salle est libre ? ». `BookAsync` s'exécute donc dans une transaction qui verrouille la ligne du membre, puis celle de la salle (`SELECT … FOR UPDATE`, toujours dans cet ordre pour éviter un blocage mutuel). La seconde requête attend, puis voit la réservation de la première. Le même verrou empêche un membre Standard de dépasser sa limite avec des requêtes simultanées.

## API

| Méthode | Route | Rôle |
|---|---|---|
| GET | `/api/rooms` | liste, avec `isAvailableNow` (salle active et libre à l'instant) |
| GET | `/api/rooms/{id}` | détail |
| POST | `/api/rooms` | création |
| PUT | `/api/rooms/{id}` | modification (désactivation avec `isActive: false`) |
| GET | `/api/members`, `/api/members/{id}` | liste, détail |
| POST | `/api/members` | création |
| PUT | `/api/members/{id}` | modification |
| GET | `/api/reservations?roomId=&date=&includeCancelled=` | planning : confirmées, triées par début ; `date` garde les réservations qui **commencent** ce jour-là |
| GET | `/api/reservations/{id}` | détail |
| POST | `/api/reservations` | réservation (règles ci-dessus) |
| POST | `/api/reservations/{id}/cancel` | annulation (sans erreur si déjà annulée) |
| GET | `/health` | santé de l'API et de la base |

Une requête invalide répond 400 avec `{ "errors": { "Champ": ["message"] } }`. Un élément inconnu répond 404. Les erreurs non gérées répondent en `ProblemDetails` (JSON).

## Tests

```bash
cd backend && dotnet test                  # Docker doit tourner : Testcontainers lance un MySQL jetable
cd frontend && npm run lint && npm test    # ESLint, puis Vitest
cd frontend && npx next typegen && npx tsc --noEmit
cd frontend && API_BASE_URL=http://localhost:5080 npm run build
```

- **API** (xUnit) : de vrais appels HTTP contre un vrai MySQL, pas SQLite ni une base en mémoire, parce que les migrations et les verrous sont propres à MySQL. Une horloge réglable (`TestClock`) permet de tester « maintenant » et les bornes (une réservation qui finit à 14 h, une 4ᵉ réservation, exactement 4 h). Des tests lancent plusieurs réservations **simultanées** du même créneau et exigent exactement une réussite.
- **Front** (Vitest) : le client d'API (avec un `fetch` simulé) et les fonctions pures qui transforment un formulaire en requête, lisent les filtres du planning ou regroupent les réservations par jour.
- `npx next typegen` est nécessaire avant `tsc` : Next génère les types `PageProps` et `LayoutProps`.
- Il n'y a pas de test de navigateur dans le dépôt.

## Architecture

```
Navigateur ──► Next.js (:3000) ──► API ASP.NET Core (:5080) ──► MySQL (:3307)
```

Seul le serveur Next parle à l'API : il n'y a donc **pas de CORS** à configurer, et l'adresse de l'API (`API_BASE_URL`, sans préfixe `NEXT_PUBLIC_`) n'arrive jamais dans le navigateur.

```
backend/
  Api/
    Controllers/        un contrôleur par ressource (HTTP seulement)
    Services/           ReservationService : les règles de réservation
    Dtos/  Entities/    contrats de l'API et modèle de données
    Data/               AppDbContext, données de démonstration
    Migrations/
  Api.Tests/            xUnit + WebApplicationFactory + Testcontainers
frontend/
  app/                  pages et Server Actions (/, /planning, /reservations, /rooms, /members)
  components/           composants d'interface (formulaires, champs, badge, boîte de confirmation)
  lib/api-client/       client HTTP (server-only) : un fichier par ressource, types des DTOs
  lib/                  conversions de formulaires, filtres du planning, libellés français
```

Le contrôleur lit la requête et répond ; le service décide. Les contrôleurs des salles et des membres n'ont pas de service : ce sont des opérations simples.

## Choix et limites

- **Heures locales, sans fuseau** : l'espace est sur un seul site. Les dates sont stockées et échangées en heure locale (`2026-11-20T14:00:00`). « Maintenant » vient d'une horloge injectée (`TimeProvider`), jamais de `DateTime.Now`.
- **« À venir »** veut dire « pas encore commencée » : une réservation en cours ne compte pas dans la limite de 3.
- **Annuler** est toujours possible, même pour une réservation passée, et annuler deux fois n'est pas une erreur.
- **Pas d'authentification** : tout le monde peut tout faire. Le sujet n'en demande pas ; il faudrait en ajouter une avant toute mise en ligne.
- **EF Core 9 sur un projet .NET 10** : Pomelo n'a pas encore de version pour EF Core 10. `MySqlConnector` est épinglé en 2.6.2, car la 2.4.0 que Pomelo tire par défaut a une vulnérabilité connue (GHSA-473q-m89c-ghf8).
- Pas de CI ni de Dockerfile : Docker ne sert qu'à MySQL. Les types du front sont écrits à la main : pour un gros projet, générez-les depuis `/openapi/v1.json`.
- Conventions : identifiants et commentaires en anglais, textes affichés en français, commits en [Conventional Commits](https://www.conventionalcommits.org/).

## Pièges Next.js 16 déjà rencontrés

`next.config.ts` active `cacheComponents` et `partialPrefetching`. Conséquences :

- **Toute lecture de l'API doit être sous une frontière `<Suspense>`.** Le `loading.tsx` racine couvre les listes (`/`, `/members`, `/planning`). Les pages qui affichent un titre avant leurs données (`/reservations/new`, les pages de modification) lisent `params` ou `searchParams` dans un composant enfant, dans son propre `<Suspense>`.
- **La navigation** utilise `usePathname()` : elle est dans un `<Suspense>` (`components/main-nav.tsx`), sinon `next build` échoue sur les routes dynamiques.
- `params` et `searchParams` sont des promesses : `await`.
- `redirect()` fonctionne en levant une exception : appelez-le **hors** des `try/catch`.
- **Un `<select>` perd son choix après une erreur de formulaire** si on ne le recrée pas : React réinitialise le formulaire après chaque Server Action sans mettre à jour l'option sélectionnée. `SelectField` a donc une `key` égale à sa valeur par défaut.
- Une page « introuvable » répond en HTTP 200, car la page est streamée avant que l'API réponde.
- `next build` appelle l'API pendant le rendu : `API_BASE_URL` doit être définie, sinon le build échoue.
- Si une modification de style n'apparaît pas dans un build, supprimez `frontend/.next` : le cache de build peut être périmé.
- Le fichier `frontend/AGENTS.md` est généré par `next dev` : gardez-le versionné pour ne pas recréer une modification à chaque lancement.
