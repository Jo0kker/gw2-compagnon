# Compagnon Guild Wars 2 — nom à définir

Application Windows x64 autonome. **Première tranche de développement, pas encore une version utilisable avec GW2.**

## Direction retenue

Maquette 01 simplifiée : **aucun menu latéral**, navigation compacte en haut, deux tableaux par défaut, pas de diagramme circulaire ni d’historique ajouté au dashboard. Réglages visibles en mode édition.

[Voir la maquette révisée](docs/maquettes/01-dashboard-simplifie.png). Image de conception, pas capture de l’application.

## Implémenté dans le code

- Solution .NET 10, cœur C# indépendant de WPF et shell de bureau Windows.
- Profils : création vide, renommage, duplication profonde, suppression avec confirmation ; création, renommage, duplication et suppression des pages.
- Widgets : ajout, duplication, déplacement/redimensionnement à la souris ou au clavier, masquage, retrait et filtres indépendants.
- Tableaux joueurs et sous-groupes calculés sur un combat **fictif** ; désactiver le rejeu conserve widgets et réglages.
- Récepteur Named Pipe et simulateur séparé : échanges locaux réels de données fictives, sessions, détection des pertes/coupures et conservation des dernières valeurs. **La DLL ArcDPS n’est pas encore implémentée.**
- Import/export portable (JSON, 1 Mo maximum) : identifiants régénérés, disposition conservée, validation avant import et liste blanche des champs exportés. Les réglages opaques des widgets inconnus ne sont pas repris.
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

Pour tester la connexion locale : lancer l’application, choisir **Utiliser le simulateur local** dans Intégrations, puis `dotnet run --project tools/Companion.BridgeSimulator` dans un second terminal. Options `--drop`, `--interrupt` et `--stall` pour éprouver les erreurs. Voir le [guide de transport local](docs/10-transport-local.md).

Le workflow Windows prépare aussi un artefact `gw2-companion-windows-x64-preview-…` avec le runtime .NET inclus, téléchargeable depuis GitHub Actions **si le workflow réussit**. Extraire tout le ZIP puis lancer `Companion.Desktop.exe`. Ce paquet de test n’est pas signé et n’intègre pas encore l’auto-update. Aucun artefact de cette nouvelle configuration n’a encore été vérifié.

## Validation et limites

**18/18 scénarios passent sous Linux** : profils, stockage, navigation, protocole, échanges sur un vrai pipe, pertes, déconnexion et timeout. Le simulateur compile sans avertissement. XML XAML bien formé. **Compilation WPF non vérifiée** : restauration des références Windows bloquée ici par le proxy sur `api.nuget.org` (NU1301/HTTP 403). Les résultats de la CI distante n’ont pas pu être consultés ; la disponibilité des paquets dépend de son succès. Aucun lancement ni essai visuel Windows ; voir [design-qa.md](design-qa.md).

Encore à implémenter : bridge et données réelles, inventaire/installation ArcDPS, logs/sessions, WvW Insights, profils extensibles aux réglages d’intégrations tierces, restauration de position multi-écran et grille magnétique. Les onglets concernés expliquent leur indisponibilité. Aucun fichier du jeu modifié, aucun log envoyé.

## Conception et sources

1. [Produit, écrans et parcours](docs/01-produit-et-parcours.md)
2. [Architecture](docs/02-architecture.md)
3. [Profils, intégrations et widgets](docs/03-modele.md)
4. [Bridge et reprise](docs/04-bridge.md)
5. [Installation et coexistence](docs/05-installation.md)
6. [Logs et WvW Insights](docs/06-publication.md)
7. [Matrice et sources](docs/07-sources-et-integrations.md)
8. [Réalisation et validation](docs/08-realisation.md)
9. [Livraison Windows et auto-update](docs/09-livraison-et-auto-update.md)
10. [Transport local et simulateur implémentés](docs/10-transport-local.md)
11. [Premier essai Windows : récupération et vérifications](docs/11-premier-test-windows.md)

Cible : .NET 10/WPF, SQLite, bridge C++ x64 et Named Pipes. Ni Blish HUD ni Nexus requis pour la fonction principale. Les API officielles ArcDPS/WvW Insights et les règles ArenaNet restent à relire depuis les domaines bloqués ici. Aucune intégration validée en jeu.

## Langues et licence

Interface anglais/français : choisir la langue dans Paramètres, puis redémarrer. Licence MIT. Documentation de contribution et de signature : [English README](README.md). La signature SignPath est préparée, mais non activée ; aucune admission à la fondation n’est acquise.
