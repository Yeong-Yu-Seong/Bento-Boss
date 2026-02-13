# Bento Boss

**A VR canteen simulation built to help kids practise financial literacy.**

## About

Made by the **Jellyfish** team, Bento Boss is set in a school stall where players prepare food, handle payments, and manage food stock while serving a queue of students.

* **Target Audience:** Ages 7–14
* **Purpose:** Teach money counting, change giving, and simple resource management skills.
* **Web Integration:** [Link](https://ip-y2-2.web.app)

---

## Game Loop & Mechanics

**The Core Loop**
Students Order → Player Serves → Student Pays → Player Returns Change → Repeat.

**Restocking System**
Players must manage inventory using physical VR interactions. When a food item runs out:

1. **Retrieve Crate:** Locate the crate color corresponding to the food item.
* **Yellow** 🟡 : Fruits
* **Blue** 🔵 : Drinks
* **Red** 🔴 : Bentos


2. **Open:** Physically grab and remove the lid of the crate to reveal the stock.
3. **Disposal:** Once the crate is empty, the player must physically grab the crate and place it in the trash bin to clear the workspace.

---

## Key Features

* **VR Interactions:** Physics-based handling of food, money, and crates.
* **Order System:** Randomized student orders.
* **Money Checking:** System to validate payments and calculated change.
* **Score Tracking:** Performance tracking with difficulty scaling.
* **Accessibility:** Includes Light and Dark mode options.
* **Documentation:** In-game Guide and Handbook.

---

## Controls

| Action | Input |
| --- | --- |
| **Move** | VR Joystick |
| **Grab / Interact** | Controller Trigger |
| **Navigate UI** | Controller Raycast |

---

## Installation & Requirements

**How to Install**

1. Download the latest `.apk` build.
2. Sideload or open the file on your headset.
3. Launch from the library.

**System Requirements**

* **OS:** Windows 10 or higher
* **Memory:** 8GB RAM
* **Hardware:** VR-ready PC & Meta Quest headset

---

## Known Issues

Please be aware of the following bugs in the current Beta build:

* **Critical Crash:** Attempting to dispose of a restock crate while it still contains food items will cause the game to crash. Please ensure crates are completely empty before disposal.
* **Physics Glitch:** Walking through the cash register mesh causes the cash tray to detach and function incorrectly.
* **Visual Artifacts:** Rain effects occasionally clip through the ceiling and appear inside the canteen environment.

---

## Future Plans

* Adjustable difficulty settings.
* New student character models.
* Expanded food menu and variety.
* Enhanced character animations.

---

## Team Jellyfish

| Role | Member(s) |
| --- | --- |
| **Backend & Programming** | Jayden |
| **XR Developer** | Jayden, Yu Seong |
| **3D Modelling** | Yu Seong, Shannon |
| **Level Design** | Shannon, Yu Seong |
| **Environmental Artist** | Yu Seong |
| **Lighting Artist** | Jayden |
| **Audio Designer** | Yu Seong |
| **Graphics & VFX** | Elly, Shannon, Yu Seong |
| **UX Researcher** | Elly |
| **QA Tester** | Jayden |

---

## Credits

* **Sounds:** [Pixabay](https://pixabay.com)
* **Environment:** [Free Trees (Unity Asset Store)](https://assetstore.unity.com/packages/3d/vegetation/trees/free-trees-103208)
* **Shaders:** [Water Shader Tutorial](https://www.youtube.com/watch?v=ILmSkM7yKD4)
* **VFX:** [Star Sticker](https://www.rawpixel.com/image/6772542/png-sticker-public-domain)
* **Assets:** City Skyline by Mr Ryan, [Vending Machine](https://mx.pinterest.com/pin/453385887481397902/)