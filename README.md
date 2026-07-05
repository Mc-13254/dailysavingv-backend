# DailySavingV.API — Backend .NET (C#)

API REST pour le système de microfinance / gestion d'épargne journalière.

## Stack
- **.NET 8** / ASP.NET Core Web API
- **Entity Framework Core 8** + SQL Server
- **JWT** (access token + refresh token en base, rotation à chaque refresh)
- **Maker-Checker** générique sur chaque entité métier
- **Filtrage automatique par agence** via les *global query filters* d'EF Core

## Démarrage rapide

1. **Exécuter le script SQL** `DailySavingV_Schema.sql` dans SQL Server Management Studio pour créer la base.
2. Ouvrir `appsettings.json` et ajuster :
   - `ConnectionStrings:DefaultConnection` (nom de votre instance SQL Server)
   - `Jwt:Key` (remplacer par une vraie clé secrète longue et aléatoire — **ne jamais commiter la vraie clé**)
3. Restaurer les packages et lancer :
   ```bash
   dotnet restore
   dotnet run
   ```
4. Swagger disponible sur `https://localhost:xxxx/swagger` (le port exact s'affiche au démarrage).

## Comment fonctionne l'isolation par agence

Le cœur de l'exigence "un collecteur ne voit que les données de son agence" est centralisé
dans **`Data/AppDbContext.cs`** :

```csharp
modelBuilder.Entity<Collector>()
    .HasQueryFilter(x => _currentUser.IsHeadOffice || x.AgenceID == _currentUser.AgenceID);
```

`ICurrentUserService` lit le claim `agenceId` du JWT à chaque requête. Résultat : **tout**
`_db.Collectors.ToListAsync()`, où qu'il soit appelé dans le code, ne retourne que les
lignes de l'agence connectée — impossible à oublier ou contourner par erreur.
Seul le rôle `ADMIN` (siège) voit toutes les agences.

Cette même mécanique est appliquée à `Client`, `Accounts`, `Transactions`, `Users`.

## Comment fonctionne le Maker-Checker

`CollectorController` est l'implémentation de référence — copiez ce pattern pour
toute nouvelle entité :

1. `POST /api/collector` → écrit dans `CollectorTMP` (jamais dans `Collector`), statut `PENDING`
2. `GET /api/collector/pending` → liste les demandes en attente (scope agence)
3. `POST /api/collector/pending/{id}/approve` → un Superviseur/Admin applique le changement en production
4. `POST /api/collector/pending/{id}/reject` → rejette avec motif

`ClientController` et `CommissionController` (pour `CommissionRange`) suivent exactement
le même schéma. Pour ajouter le Maker-Checker à une entité restante (`Contract`, `Agence`,
`IMF`...), dupliquez le contrôleur en changeant seulement les noms de types — la table
`*Tmp`/`*TMP` correspondante existe déjà dans le schéma SQL et dans `Entities/Pending/`.

## Moteur de commission automatique

`Services/CommissionService.cs` implémente exactement les 5 étapes du cahier des charges
(type de transaction → type de commission → plage correspondante → méthode de calcul →
montant). `TransactionService.cs` l'appelle systématiquement à chaque création de
transaction, stocke le résultat sur la transaction, alimente `HistCalculComis` (audit)
et rend le montant disponible pour le reçu et les tableaux de bord — aucun calcul manuel
n'est possible ni nécessaire.

## Structure du projet

```
Entities/            Classes EF Core (tables de production)
Entities/Pending/    Classes EF Core (tables Tmp / Maker-Checker)
Data/                AppDbContext + configuration des filtres par agence
Services/             AuthService, CommissionService, TransactionService, CurrentUserService
Controllers/          Endpoints REST
```

## Prochaine étape

Le frontend React (calqué sur vos maquettes : Agency/Users/Collector/Client/Account/
Contract/Commission/IMF Management) consommera cette API. Voir le dossier `frontend/`
une fois généré.
