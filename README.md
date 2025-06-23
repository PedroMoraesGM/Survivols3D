# Survivals# Survivals

## Project Overview

**Survivals** is a 3D action-survival game built with Photon Quantum. The project uses a deterministic simulation for multiplayer gameplay, with a modular ECS (Entity Component System) architecture. Below is a summary of the main systems and their roles in the project.

---

## Main Systems

### 1. **MoveSystem**
Handles player movement and camera look logic. Processes player input, applies movement speed, and ensures responsive, character-like controls. Also manages vertical look limits and clamps player height to the ground.

### 2. **ShootingSystem**
Manages all weapon firing logic. Supports burst fire, cooldowns, and firing multiple projectile types per weapon. Instantiates projectiles and applies weapon upgrades and effects.

### 3. **EnemyAISystem**
Controls enemy behavior, including movement, targeting the closest player, attacking, and responding to status effects. Integrates with the player registry to find and pursue targets.

### 4. **EnemySpawnSystem**
Handles spawning of enemy waves, batch sizes, and spawn intervals. Ensures the correct number of active enemies and manages spawn positions relative to players.

### 5. **AreaWeaponSystem**
Processes area-based effects (e.g., healing domes, slow fields). Each area effect is a separate entity, allowing for multiple overlapping effects (like healing and slowing) with different collision layers.

### 6. **HomingProjectileSystem**
Controls the logic for projectiles that home in on targets. Supports features like not repeating targets, reacquiring new targets after a hit, and handling homing behavior for different projectile types.

### 7. **ApplyUpgradeSystem**
Applies upgrades to players and their weapons. Manages upgrade acquisition, stacking, and the effects of upgrades on weapon and player stats.

### 8. **CollisionSystem**
Handles collision and trigger events between entities (e.g., picking up XP, projectiles hitting targets, area effects applying to characters).

### 9. **UpgradeUIController**
Manages the UI for upgrades, displaying acquired upgrades, their cooldowns, and visual feedback for upgrade effects.

---

## System Architecture Notes

- **ECS Pattern:** Each system operates on entities with specific components, enabling modular and scalable gameplay logic.
- **Upgrades:** Upgrades are tracked per player and can affect weapons, stats, and unlock new abilities.
- **Weapons & Effects:** Multiple weapons or area effects are handled by spawning separate entities, each with their own logic and components.
- **Networking:** Photon Quantum ensures deterministic simulation for all players, keeping gameplay in sync.

---

## How to Extend

- **Add new weapons:** Create new projectile prefabs and add them to the weapon's `ProjectilePrefabs` list.
- **Add new upgrades:** Define new upgrade effects in the upgrade data and handle them in `ApplyUpgradeSystem`.
- **Add new enemy types:** Create new enemy prototypes and implement their AI logic in `EnemyAISystem`.

---

For more details, see the code in the `Assets/QuantumUser/Simulation/Systems`