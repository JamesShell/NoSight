# The Walking Deaf

A top-down pixel art shooter built in **Unity** for **Global Game Jam 2025**.

In *The Walking Deaf*, sight is limited, sound is dangerous, and every shot matters. You explore dungeon-like levels using an **echo-based fog of war** system to reveal the map around you. The catch? Making noise helps you understand the world — but it also helps the zombies find **you**.

---

## About the Game

*The Walking Deaf* is a top-down survival shooter where the player navigates through a series of dungeon-style levels filled with zombie waves. Instead of seeing the full map at once, you uncover your surroundings through an **audio-inspired reveal mechanic** — basically “listening” with echoes to expose parts of the map hidden by fog of war.

Each new level throws fresh enemy waves at you and rewards progress with a stronger weapon. You begin with a **pistol**, then move on to weapons like the **rifle**, **shotgun**, and more depending on the build.

The main tension of the game comes from a simple tradeoff:

- **Shoot to survive**
- **Make noise and attract more zombies**
- **Use echoes to reveal the map**

---

## Core Features

- **Top-down pixel art shooter gameplay**
- **Fog of war / map reveal system** based on echo-listening
- **Dungeon-style level progression**
- **Wave-based zombie spawns** on each level
- **Weapon progression** as you advance
- **Risk/reward combat loop** where gunfire draws enemies in
- Built in **Unity** for **GGJ 2025**

---

## Gameplay Loop

1. Enter a new dungeon level
2. Use echoes to reveal nearby areas hidden by fog
3. Fight off zombie waves
4. Survive long enough to clear the level
5. Unlock a new weapon
6. Go deeper and deal with harder spawns

---

## Fog of War / Echo Mechanic

One of the main ideas behind *The Walking Deaf* is using **echo** as a way to understand your surroundings.

Instead of giving the player constant full vision of the map, the game hides unexplored areas behind fog of war. You reveal the environment by sending out an echo-like pulse, creating a playstyle where awareness has to be earned.

This mechanic ties directly into the game’s theme and makes navigation feel tense, especially when you know enemies can close in while you’re trying to figure out what’s ahead.

---

## Controls

- **WASD** — Move
- **Mouse** — Aim
- **Left Click** — Shoot / Reveal map

---

## Project Structure

Example structure if you want to document the repo a bit:

```text
Assets/
├── Scripts/
├── Prefabs/
├── Scenes/
├── Sprites/
├── Audio/
└── UI/
```

---

## Running the Project

1. Clone the repository
2. Open it in the correct Unity version
3. Load the main scene
4. Press Play in the Unity Editor

---

## Jam Context

This game was created for **Global Game Jam 2025**.

The project was made under jam constraints, so the focus was on building a strong core mechanic, a clear gameplay loop, and a tense atmosphere around limited vision, noise, and survival.

---

## Future Ideas

A few directions the project could grow in:

- More enemy types with different hearing / movement behavior
- More weapons and upgrade choices
- Smarter wave scaling
- Stronger sound design tied to the echo mechanic
- Larger procedural dungeon variation
- Better UI and feedback for map reveal and aggro range

---

## Final Note

*The Walking Deaf* is a small jam game built around a simple idea: sometimes the only way to find your path is to make enough noise for the world to answer back.
