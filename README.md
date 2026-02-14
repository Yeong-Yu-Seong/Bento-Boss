# Bento Boss
**A VR canteen simulation built to help kids practice financial literacy.**

> 📦 **[Download Latest Release (.apk)](https://github.com/Yeong-Yu-Seong/Bento-Boss/releases/latest)**

---

## About
Made by the **Jellyfish** team, Bento Boss is set in a school canteen stall where players prepare food, handle payments, and manage food stock while serving a queue of students.

- **Target Audience:** Ages 7–14
- **Purpose:** Teach money counting, change calculation, and resource management skills
- **Web Integration:** [Firebase Web Dashboard](https://ip-y2-2.web.app)

---

## Game Loop & Mechanics

### **The Core Loop**
Students Order → Player Serves → Student Pays → Player Returns Change → Repeat

### **Restocking System**
Players must manage inventory using physical VR interactions. When a food item runs out:

1. **Retrieve Crate:** Locate the crate color corresponding to the food item
   - 🟡 **Yellow:** Fruits
   - 🔵 **Blue:** Drinks
   - 🔴 **Red:** Bentos (unlocks at $15 profit)
2. **Open:** Physically grab and remove the lid of the crate to reveal the stock
3. **Disposal:** Once the crate is empty, grab and place it in the trash bin

---

## Key Features

- **VR Interactions:** Physics-based handling of food, money, and crates
- **Dynamic Order System:** Randomized student orders with anti-repeat logic
- **Money Validation:** Real-time change calculation and overpayment detection
- **Progression System:** Bento items unlock at $15 profit milestone
- **Score Tracking:** Performance grading (S to F) based on accuracy and speed
- **Accessibility:** Light and Dark mode options
- **Documentation:** In-game Guide and Handbook with session statistics

---

## Controls

| Action | Input |
|--------|-------|
| **Move** | VR Joystick |
| **Grab / Interact** | Controller Trigger |
| **Navigate UI** | Controller Raycast |

---

## Installation & Requirements

### **How to Install**
1. Download the latest `.apk` from [Releases](https://github.com/Yeong-Yu-Seong/Bento-Boss/releases)
2. Enable **Developer Mode** on your Meta Quest:
   - Meta Quest app → Settings → Developer Mode → Enable
3. Sideload via **SideQuest** or transfer to `/sdcard/Download/`
4. Install and launch from **Apps → Unknown Sources**

### **System Requirements**
- **VR Headset:** Meta Quest 2 / Quest 3 / Quest Pro
- **Storage:** Minimum 2GB free space
- **Play Area:** 2m x 2m recommended (room-scale VR)
- **Internet:** Required for Firebase authentication & database sync

**Note:** This is a standalone Quest app (.apk). No PC required.

---

## Known Issues

Please be aware of the following bugs in the current **Beta** build:

- **Physics Glitch:** Walking through the cash register mesh causes the cash tray to detach and function incorrectly
- **Rain VFX Clipping:** Rain particles occasionally clip through the ceiling into the canteen

---

## Future Plans

- Adjustable difficulty settings
- Diverse student character types
- Expanded food menu variety
- Enhanced character animations and feedback

---

## Team Jellyfish

| Role | Member(s) |
|------|-----------|
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

- **Sounds:** [Pixabay](https://pixabay.com)
- **Trees:** [Free Trees (Unity Asset Store)](https://assetstore.unity.com/packages/3d/vegetation/trees/free-trees-103208)
- **Water Shader:** [Tutorial by Brackeys](https://www.youtube.com/watch?v=ILmSkM7yKD4)
- **Star VFX:** [Rawpixel](https://www.rawpixel.com/image/6772542/png-sticker-public-domain)
- **City Skyline:** Mr Ryan
- **Vending Machine Reference:** [Pinterest](https://mx.pinterest.com/pin/453385887481397902/)

---

## License

Educational project developed for Ngee Ann Polytechnic's Integrated Project (Year 2, Semester 2.2).  
© 2026 Team Jellyfish. All rights reserved.