# Premier essai sous Windows

## Récupérer les paquets

Dans GitHub → Actions → Build and verify, ouvrir l’exécution correspondant au dernier commit de `main`. **Attendre la réussite du job Windows.** Les paquets de cette exécution sont disponibles en bas de page dans Artifacts :

- `gw2-companion-windows-x64-preview-<commit>` : l’application ;
- `gw2-bridge-simulator-windows-x64-<commit>` : l’émetteur de données fictives.

Ils incluent le runtime .NET : pas de SDK à installer. Extraire chaque archive entièrement dans son propre dossier, puis ouvrir `Companion.Desktop.exe`. Ce sont des paquets de développement non signés, sans auto-update pour le moment. Les artefacts expirent après 14 jours ; le workflow peut être relancé manuellement.

Si le job échoue ou qu’aucun artefact n’apparaît, le paquet n’a pas été produit : les logs GitHub Actions sont nécessaires pour diagnostiquer le build. Un workflow présent dans le dépôt ne garantit pas sa réussite.

## Essais prioritaires

1. Vérifier que la fenêtre s’ouvre et que la source est explicitement fictive. Aucun besoin de démarrer GW2 : cette tranche ne touche pas le jeu.
2. Cliquer Modifier le dashboard ; déplacer/redimensionner un widget, puis changer le filtre de sous-groupe sur un seul des deux tableaux. Vérifier leur indépendance.
3. Créer un profil, une page, puis les dupliquer et les renommer. Supprimer une copie ; l’original doit rester intact. Masquer un widget et le réafficher en mode édition.
4. Exporter le profil vers un nouveau fichier JSON puis l’importer deux fois : les copies doivent rester indépendantes. Un fichier invalide doit être refusé sans modifier les profils existants.
5. Terminer l’édition, fermer l’application et la rouvrir ; vérifier noms, pages, filtres, visibilité et disposition. La page active et la position de la fenêtre ne sont pas encore mémorisées.
6. Dans Intégrations, activer Utiliser le simulateur local, puis lancer `Companion.BridgeSimulator.exe` sous le même utilisateur. Les dégâts évoluent pendant dix secondes puis restent affichés comme historiques.
7. Dans un terminal ouvert dans le dossier du simulateur, lancer `./Companion.BridgeSimulator.exe --drop`, puis `--interrupt` et `--stall` séparément. Les chiffres ne doivent pas être remplacés par de faux zéros ; les pertes/coupures doivent être visibles.
8. Déplacer la fenêtre sur le deuxième écran, tester la taille du texte et les contrôles à 100/150/200 % de mise à l’échelle. La récupération automatique après débranchement reste à implémenter.

## Informations utiles pour un retour

Noter le commit/nom de l’artefact, la version Windows, le réglage DPI, les étapes exactes et le texte de l’erreur. Les profils sont dans `%LOCALAPPDATA%/GW2Companion/profiles.json`, avec une copie précédente `.bak`. Ne pas joindre de jeton ni de logs de jeu privés pour ces essais.

## Limites connues

Pas de DLL ArcDPS, de données réelles, d’installation d’addons, d’indexation des logs, de publication WvW Insights ni de moteur d’auto-update. Les profils portables prennent en charge les réglages actuels ; pour un type de widget inconnu, l’emplacement est préservé mais les réglages opaques sont exclus. Le rendu WPF n’a pas encore été observé dans l’environnement de développement Linux.
