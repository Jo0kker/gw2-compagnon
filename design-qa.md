# Vérification de l’interface

final result: blocked

Référence : maquette 01 choisie, simplifiée sans menu latéral dans `docs/maquettes/01-dashboard-simplifie.png`. Variante présentée avant implémentation.

Le XAML définit une navigation supérieure, deux tableaux, un sélecteur profil/page, un indicateur de source fictive et un mode édition. Les profils et filtres sont reliés au domaine et au stockage local.

Vérifications exécutées : sept scénarios du cœur et du modèle de présentation compilés/exécutés sous .NET 10 Linux, réussis ; parsing XML des deux fichiers XAML, réussi. Le parsing XML ne remplace pas une compilation XAML.

Compilation Desktop tentée : bloquée lors de la restauration des références WindowsDesktop via NuGet (proxy HTTP 403 / NU1301). Aucune capture de rendu WPF : environnement Linux, sans session Windows.

À vérifier sous Windows : compilation, lancement et captures en consultation/édition à 1280×850 et 1440×1024, DPI 100/150/200 %. Tester profils/pages, déplacement, redimensionnement, clavier, masquage, filtres, sauvegarde/rechargement et désactivation du rejeu. Contrôler les erreurs de binding WPF.

Écarts délibérés pour cette tranche : ComboBox natifs, tableaux textuels sans barres décoratives, canvas défilable sur petite fenêtre. Contraste des états sélectionnés/focus à contrôler au rendu. Dimensions initiales fixes, adaptation complète encore à réaliser.

La maquette ne prouve ni rendu réel ni fonctionnement de l’exécutable. La QA visuelle n’est pas déclarée réussie.
