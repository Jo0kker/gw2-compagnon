# Compagnon Guild Wars 2 — nom à définir

Application Windows x64 autonome. **Première tranche de développement, pas encore une version utilisable avec GW2.**

## Direction retenue

Maquette 01 simplifiée : **aucun menu latéral**, navigation compacte en haut, deux tableaux par défaut, pas de diagramme circulaire ni d’historique ajouté au dashboard. Réglages visibles en mode édition.

[Voir la maquette révisée](docs/maquettes/01-dashboard-simplifie.png). Image de conception, pas capture de l’application.

## Implémenté dans le code

- Solution .NET 10, cœur C# indépendant de WPF et shell de bureau Windows.
- Profils : création vide, renommage, duplication profonde, suppression avec confirmation ; ajout et sélection de pages.
- Widgets : ajout, duplication, déplacement/redimensionnement à la souris ou au clavier, masquage, retrait et filtres indépendants.
- Tableaux joueurs et sous-groupes calculés sur un combat **fictif** ; désactiver le rejeu conserve widgets et réglages.
- Profils persistants dans `%LOCALAPPDATA%/GW2Companion/profiles.json`, validation et remplacement atomique avec copie précédente `.bak`. Un fichier invalide n’est pas écrasé silencieusement.
- Tests exécutables sans dépendance NuGet et workflow CI Linux/Windows.

Le stockage JSON est une première étape locale. SQLite reste prévu pour logs, sessions et transactions ; une migration sera nécessaire. Aucun secret ne fait partie du modèle actuel.

## Lancer sous Windows

Installer le **SDK .NET 10**, puis depuis la racine :

```powershell
dotnet run --project src/Companion.Desktop
```

Le combat fictif est activé à la première ouverture et toujours signalé. Ouvrir **Modifier le dashboard** pour organiser les widgets. Enregistrement en terminant l’édition et à la fermeture ; les commandes de disposition enregistrent aussi immédiatement. Les poignées acceptent les flèches du clavier (16 DIP par pas).

```powershell
# Tests du domaine et du modèle de présentation, Windows ou Linux
dotnet run --project tests/Companion.Core.Tests --configuration Release

# Compilation de l’application sur Windows
dotnet build src/Companion.Desktop --configuration Release
```

Les tests sont un exécutable de scénarios : utiliser `dotnet run`, pas `dotnet test`.

## Validation et limites

**7/7 scénarios passent sous Linux** : filtres, doublons, pertes, duplication, stockage et navigation profils/pages. XML XAML bien formé. **Compilation WPF non vérifiée** : restauration des références Windows bloquée ici par le proxy sur `api.nuget.org` (NU1301/HTTP 403). Workflow CI Windows écrit mais non exécuté. Aucun lancement ni essai visuel Windows ; voir [design-qa.md](design-qa.md).

Encore à implémenter : bridge et données réelles, inventaire/installation ArcDPS, logs/sessions, WvW Insights, import/export, restauration de position multi-écran, grille magnétique et édition complète des pages. Les onglets concernés expliquent leur indisponibilité. Aucun fichier du jeu modifié, aucun log envoyé.

## Conception et sources

1. [Produit, écrans et parcours](docs/01-produit-et-parcours.md)
2. [Architecture](docs/02-architecture.md)
3. [Profils, intégrations et widgets](docs/03-modele.md)
4. [Bridge et reprise](docs/04-bridge.md)
5. [Installation et coexistence](docs/05-installation.md)
6. [Logs et WvW Insights](docs/06-publication.md)
7. [Matrice et sources](docs/07-sources-et-integrations.md)
8. [Réalisation et validation](docs/08-realisation.md)

Cible : .NET 10/WPF, SQLite, bridge C++ x64 et Named Pipes. Ni Blish HUD ni Nexus requis pour la fonction principale. Les API officielles ArcDPS/WvW Insights et les règles ArenaNet restent à relire depuis les domaines bloqués ici. Aucune intégration validée en jeu.
