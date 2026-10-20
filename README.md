# 🍣 Tokyo Express

**Tokyo Express** est un jeu de livraison hyper-casual dynamique et coloré développé sous **Unity 6 (URP)**, inspiré de l'esthétique et des mécaniques de *Rail Express*.

Le joueur contrôle un livreur en moto à travers les rues de Tokyo en vue top-down plongeante, naviguant sur des rails et splines de route pour livrer ses commandes à temps tout en esquivant les obstacles et le trafic urbain.

---

## 🎮 Gameplay & Fonctionnalités Clés

- **Contrôle One-Touch (Touch & Swipe)** :
  - **Swipe** : Sélection de direction aux carrefours et embranchements. Le joueur ne peut pas dévier involontairement en pleine ligne droite.
  - **Tap** : Saut pour franchir les obstacles ou véhicules du trafic, et activation de capacités spéciales.
- **Routes & Rails basés sur les Splines (`UnityEngine.Splines`)** :
  - Déplacement fluide et précis des véhicules (joueur et trafic) suivant les splines et courbes de Bézier.
  - Virages arrondis esthétiques ("pop") et carrefours avec arrêt automatique et flèches de sélection de trajectoire.
- **Système de Missions de Livraison** :
  - **Food** : Décompte chronométré avec détérioration si le délai expire.
  - **Fragile** : Tolérance 0 collision (échec instantané en cas d'impact).
  - **Standard & VIP** : Objectifs de points et multiplicateurs de récompenses.
- **Progression, Garage & Monétisation** :
  - Motos déblocables avec compétences uniques (*HighJump*, *BulletTime*, *NitroBoost*, *ShieldArmor*).
  - Skins et personnalisation.
  - Sauvegarde locale robuste (`SaveSystem`).
  - Intégration publicitaire simulée (Rewarded Revive, Double Gains) et IAP "No Ads".
- **Génération Procédurale & Level Editor** :
  - Intégration Tessera (Wave Function Collapse) pour la génération de villes procédurales.
  - Éditeur de niveaux sur grille (`TokyoLevelEditorWindow`).
- **Audio Procédural** :
  - Synthèse sonore procédurale intégrée (`TokyoAudioManager`) pour les effets (moteur, saut, crash, swipe, klaxon, validation de livraison).
- **Graphismes Pop & Caméra Top-Down** :
  - Vue plongeante centrée sur le joueur avec lissage de suivi.
  - Rendu URP avec Post-Processing et Anti-Aliasing (SMAA) activés.
  - Palette colorée vibrante (indigo, cyan néon, vert lime, lilas).

---

## 🚀 Installation & Démarrage Rapide

Le projet intègre un outil d'installation et de configuration automatique en un clic :

1. Ouvrez le projet dans **Unity 6 (6000.4.x)**.
2. Dans la barre de menus, ouvrez :
   ```text
   Tokyo Express > 🛠 Fenêtre d'Installation (Setup Wizard)
   ```
3. Cliquez sur **⚡ INSTALLATION COMPLÈTE EN 1 CLIC ⚡** :
   - Configure automatiquement les **Tags & Layers** requis (`Obstacle`, `TrafficMoto`, `Road`, `PlayerMoto`, etc.).
   - Génère les ScriptableObjects et la bibliothèque de Prefabs URP.
   - Construit et ouvre la scène de démo `TokyoExpress_DemoScene.unity`.
4. Appuyez sur **Play** pour jouer immédiatement !

---

## 📂 Architecture du Projet

```text
Assets/_TokyoExpress/
├── Audio/            # TokyoAudioManager et générateur audio procédural
├── Core/             # Définitions des types de colis et runner de missions
├── Data/             # ScriptableObjects (Bikes, Skins, SaveSystem)
├── Delivery/         # Logique des missions, timers et règles d'impact
├── Editor/           # Fenêtres d'installation, Level Editor et Scene Builders
├── Grid/             # Découpage en tuiles, splines Bézier, pont Tessera WFC
├── Monetization/     # Gestionnaire IAP et Rewarded Ads simulés
├── Player/           # Contrôleur moto sur Spline, input One-Touch, Caméra Top-Down
├── Scenes/           # Scènes de démo du jeu
└── UI/               # HUD de livraison, compteurs, modales de victoire et crash
```

---

## 🛠 Prérequis

- **Unity 6** (6000.4.11f1 ou version supérieure recommandée)
- **Universal Render Pipeline (URP)**
- **Unity Splines Package** (`com.unity.splines`)
- **Input System Package** (`com.unity.inputsystem`)
