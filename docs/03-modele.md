# Profils, intégrations et widgets

Contrats conceptuels v0, à formaliser en types C# et schémas JSON lors du socle.

## Agrégats

| Entité | Champs et invariants |
|---|---|
| Profile | id UUID, schemaVersion, name, iconKey, pages, presentation, publicationPolicy ; aucune clé ni URL privée |
| Page | id, name, layoutLocked, widgets ; positions conservées même si source absente |
| WidgetInstance | id, integrationId, widgetTypeId, settingsVersion, settings, x/y/width/height en DIP, hidden ; réglages propres à l’instance |
| Presentation | density, textScale, theme ; valeurs bornées |
| PublicationPolicy | manual par défaut, enabled=false pour automatisation ; aucune destination privée dans l’export |
| IntegrationManifest | id, version, author, officialSource, dependencies, dataCapabilities, widgetDefinitions, compatibilityEvidence, installationRecipeVersion |
| Dependency | integrationId, versionRange, required/optional, purpose ; résolution détectant cycles et conflits |
| WidgetDefinition | id, integrationId, settingsSchema, settingsSchemaVersion, requiredCapabilities, rendererKey, supportedPresentations |
| CompatibilityEvidence | GW2 build, ArcDPS build, bridge version, architecture, testedAt, scenario, result, evidenceRef |
| ComponentInstallation | componentId, installedVersion, managedFiles, owner, installedHash, enabled, operationId |
| DataSnapshot | source, sessionId, encounterId, periodStart/end, capturedAt, receivedAt, scope, quality, value nullable |
| LogRecord | id, sha256, paths, size, format, mode, stability, metadata nullable, parseStatus |
| Session | id, name, ordered logIds, excluded logIds ; indépendante d’un profil |
| PublicationJob | id, connectorId/version, fingerprint, logHashes, optionsSnapshot, state, remoteJobId nullable, reportRefs, timestamps |

Le nom affiché n’est jamais une clé technique. Un auteur ou une source officielle n’implique pas une licence de redistribution. Les intégrations du catalogue sont déclaratives et contrôlées par l’application ; aucun code arbitraire téléchargé pour les widgets en P0.

## Exemple de profil sans secret

```json
{
  "schemaVersion": 1,
  "id": "profile-example",
  "name": "Ma soirée McM",
  "iconKey": "squad",
  "presentation": { "density": "comfortable", "textScale": 1.25 },
  "publicationPolicy": { "mode": "manual" },
  "pages": [{
    "id": "page-example", "name": "Vue d’ensemble", "layoutLocked": false,
    "widgets": [
      {
        "id": "damage-squad", "integrationId": "companion.arcdps",
        "widgetTypeId": "damage-table", "settingsVersion": 1,
        "x": 0, "y": 0, "width": 704, "height": 432, "hidden": false,
        "settings": { "metric": "damagePerSecond", "scope": "squad", "period": "encounter", "rows": 10 }
      },
      {
        "id": "damage-group", "integrationId": "companion.arcdps",
        "widgetTypeId": "damage-table", "settingsVersion": 1,
        "x": 720, "y": 0, "width": 480, "height": 432, "hidden": false,
        "settings": { "metric": "damagePerSecond", "scope": "subgroup", "subgroup": 2, "period": "encounter", "rows": 5 }
      }
    ]
  }]
}
```

Identifiants abrégés pour la lisibilité de l’exemple ; l’implémentation utilisera des UUID. Les préférences machine (écrans, chemins) sont séparées du profil portable.

## États orthogonaux

- Installation : absent / present / staged / pendingGameExit / applying / failed / recoveryRequired.
- Activation : enabled / disabled.
- Compatibilité : verified / unknown / knownIncompatible ; indépendante de « à jour ».
- Fonctionnement : waiting / connected / stale / disconnected / unavailable.
- Qualité : complete / partial / unknown. Valeur null signifie indisponible ; valeur 0 signifie mesure valide sans contribution.
- Origine : live / localReplay / localAnalysis / remoteReport. Rejeu synthétique ajoute `isSynthetic=true`.

Un widget exige les capacités déclarées, pas seulement la présence d’une DLL. Quand l’intégration manque, un placeholder occupe les mêmes coordonnées, conserve le type et les réglages et propose d’ouvrir la fiche.

## Duplication, import, export et migration

Duplication profonde de profil/page/widget ; régénérer tous les IDs correspondants et les références internes. Interdire le partage d’objets de réglages mutables. Modifier un filtre dans une copie ne touche jamais la source.

À l’import : taille maximale (proposition 1 Mo), parse sans exécution, validation des types/versions/longueurs/positions/nombres finis, limite de 50 pages et 200 widgets par profil, contrôle des identifiants et références ; migrer une copie puis enregistrer atomiquement. Une version future non supportée doit être rejetée avec motif, sans modifier les profils existants. Rendre les intégrations absentes visibles sans déclencher de téléchargement.

À l’export : construire un DTO portable par **liste blanche**, sans sérialiser les entités de stockage. Pour chaque widget, exporter seulement les champs déclarés `portable=true` par son schéma ; aucune valeur de credential, chemin personnel, endpoint privé ou règle de publication sensible. Les secrets n’entrent jamais dans les réglages widgets. Pour une intégration inconnue dont le schéma n’est pas disponible, préserver l’emplacement et les identifiants de type mais omettre les réglages opaques de l’export avec avertissement. Cela évite de promettre simultanément portabilité arbitraire et absence de secret.

## Définition initiale de la métrique dégâts

Ne pas sommer naïvement tous les callbacks local et area : ils peuvent recouvrir les mêmes dégâts. L’adaptateur ArcDPS doit sélectionner un flux et normaliser les événements en dégâts appliqués, en distinguant directs/altérations, cibles, propriétaires des familiers et changement d’agent. Cette logique attend l’API officielle et les fixtures validées.

Le domaine reçoit `DamageApplied(eventId, combatTimeMs, sourceAgent, ownerAgent?, subgroup?, targetAgent, targetKind, amount, damageKind)` et des bornes de combat explicites. DPS par rencontre = somme des dégâts retenus / durée de rencontre en secondes, même dénominateur pour les joueurs ; durée nulle ou combat sans borne fiable → null. Un futur « DPS actif » est une autre métrique. Les fenêtres glissantes et les cibles doivent afficher leur période/filtre. Un sous-groupe inconnu n’est pas assimilé au groupe 0. Les ennemis comptés sont ceux observés dans les échanges, jamais tous les joueurs proches.
