# Art Direction Specification — Tomorrow's Crime
## Step 7: Complete Visual Reboot (Modern Cluedo-Inspired Art Direction)

---

## 1. Executive Summary & Core Mission

*Tomorrow's Crime* is a first-person time-loop detective mystery set in **Meridian House**, an opulent private boutique hotel caught in a five-minute temporal recurrence (12:00 AM – 12:05 AM).

Prior to this visual reboot, the game's presentation relied on graybox primitives (cubes, spheres, and a capsule character), flat single-color materials, and a stark charcoal/slate IMGUI interface. 

This document defines the complete visual reboot of *Tomorrow's Crime*. The visual identity is reimagined from the ground up, drawing direct inspiration from the modern, critically acclaimed **CLUEDO / CLUE (2023)** reboot by Hasbro and its official digital adaptation by **Marmalade Game Studio**.

The goal is to translate the high-level visual qualities of modern Cluedo into an original, immersive first-person 3D mystery experience: **distinctive stylized characters, richly appointed architectural interiors, confident jewel-toned palettes, tactile materials, expressive theatrical lighting, and a polished case-dossier interface.**

---

## 2. Reference Research & Analysis: Modern Cluedo

### 2.1 The Reference Material
Observations and architectural principles were derived directly from official reference sources:
- **Hasbro 2023 Reimagined CLUE Board Game Launch**: Refresh of the classic 1949 IP introducing updated suspect backstories, a newly rendered Tudor Mansion, bespoke sculpted metal tokens, and rich narrative card illustrations.
- **Marmalade Game Studio 2023 Digital Adaptation (*Clue / Cluedo*)**: Full 3D animated reimagining featuring stylized character sculpts, dynamic theatrical camera angles, rich architectural rooms (Lounge, Hall, Conservatory, Library), interactive digital clue sheets, and case-file UI cards.
- **Official Cluedo Press Kit & Studio Art Showcases**: Detailed character model sheets, costume design breakdowns, and room diorama renders.

### 2.2 Core Visual Principles of the Reference
1. **Stylized Semi-Realistic 3D Aesthetic:**
   - Characters and environments avoid photorealistic uncanny valley while steering clear of flat, overly cartoonish/chibi abstraction.
   - Proportions are human and intentional, featuring crisp planar facial sculpting, well-defined silhouettes, and tailored clothing cuts.
2. **Distinctive Silhouettes & Costume Storytelling:**
   - Every character is immediately identifiable by their outline and color signature alone. Costumes feature strong tailoring: sharp lapels, shoulder pads, cravats, waistcoats, belts, and bespoke accessories (brass watches, cufflinks, badges).
3. **Rich, Tactile Materiality:**
   - Surfaces celebrate distinct physical properties: polished mahogany and walnut woods with lustrous specular highlights, brushed brass and antique gold metalwork, deep velvet and tweed fabrics, patterned parquet and diamond-checker marble floors, and textured damask wallpapers.
4. **Theatrical Interior Architecture:**
   - Spaces feel historically grounded yet heightened for drama. Wainscoting, crown moldings, coffered ceilings, architectural pillars, and archways frame every camera view.
5. **Expressive Chiaroscuro Lighting:**
   - Strong interplay between **warm interior practical sources** (amber chandeliers, brass wall sconces, banker lamps at 2600K–3000K) casting soft golden pools, and **cool mystery shadows** (midnight peacock blue ambient at 5500K–6500K) seeping through exterior windows.
6. **Polished Case-File Graphic Design & UI:**
   - High typographic hierarchy pairing high-class serif display fonts (evoking golden-age mystery novels and Art Deco luxury) with crisp, readable sans-serif body text.
   - UI elements are framed like luxury stationery and tactile detective case-files: gold foil borders, warm parchment paper surfaces, wax seals, brass pins, and rich jewel-tone category chips.

---

## 3. The New Art Direction for *Tomorrow's Crime*

### 3.1 Visual Narrative & Setting: Meridian House
Meridian House is a private, grand Art Deco / Edwardian boutique hotel established in 1928, catering to wealthy patrons, diplomats, and secretive guests. At midnight, the hotel is plunged into a temporal vortex: a five-minute loop where a crime is set in motion.

The visual direction must make Meridian House feel like a real, historic, breathing location with an unsettling atmosphere of luxury and tension.

### 3.2 Visual Tone
- **Sophisticated & Theatrical:** Confident, theatrical staging; dramatic lighting; rich materials.
- **Atmospheric without Darkness:** High readability for clue discovery; avoiding pitch-black monotony by utilizing warm pools of illumination.
- **Cohesive World:** The 3D environment, the stylized characters, and the 2D investigation interface share identical color tokens, typography, and material textures.

---

## 4. Central Color System

The palette replaces the previous drab charcoal/gray palette with a confident, coordinated system of jewel tones, luxury surfaces, and high-contrast typography.

```
+-------------------------------------------------------------------------------+
|                        MERIDIAN HOUSE COLOR PALETTE                           |
+---------------------+-------------------+-------------------------------------+
| Role                | Color Name        | Hex Code   | Visual Use Case        |
+---------------------+-------------------+-------------------------------------+
| Primary Brand       | Meridian Midnight | #0D1B2A    | Title screen, UI base  |
| Primary Accent      | Polished Brass    | #D4AF37    | Gold foil, highlights  |
| Secondary Accent    | Peacock Teal      | #1D4E5B    | Active states, ribbons |
| Suspect Identity 1  | Royal Concierge   | #1C2D4A    | Concierge uniform navy |
| Suspect Identity 2  | Bordeaux Crimson  | #6B1D2F    | VIP Guest wine velvet  |
| Suspect Identity 3  | Emerald Syndicate | #1B4332    | Banker / Manager green |
| Surface Base        | Dark Walnut       | #1F1712    | Wood paneling, frames  |
| Surface Card/Panel  | Aged Parchment    | #F3EBDD    | Evidence cards, dossier|
| Surface Overlay     | Slate Obsidian    | #15181E    | Semi-translucent panels|
| Text - Dark         | Iron Black        | #1A1A1D    | Text on parchment      |
| Text - Light        | Warm Ivory        | #FDFBF7    | Text on dark panels    |
| Text - Subdued      | Antique Gray      | #9E9E9E    | Timestamps, hints      |
| Status: Safe/Normal | Golden Amber      | #E5A93C    | Clock > 60s, neutral   |
| Status: Warning     | Tangelo Orange    | #E65100    | Clock 15-60s           |
| Status: Danger      | Crimson Urgency   | #C62828    | Clock < 15s, locked    |
| Status: Success     | Verdigris Green   | #2E7D32    | Unlocked, solved, pass |
+---------------------+-------------------+-------------------------------------+
```

### Color Contrast & Accessibility Rules:
- No text is rendered with less than 4.5:1 contrast against its immediate backing.
- Information (clock urgency, door status, evidence state) is never conveyed by color alone; it is always backed by iconography, text labels, and structural state changes.

---

## 5. Character Art Direction: Original Meridian House Cast

### 5.1 Design Target & Philosophy
Characters are original, stylized human models designed for first-person examination.
- **Proportions:** Realistic human skeletal proportions with stylized, sharp planar features (slight exaggeration of silhouette: broad shoulders, tailored waist, proud posture).
- **Face & Silhouette:** Expressive, clean facial plane definition, distinct hair geometry, and clear silhouette separation from the background.
- **Layered Clothing:** Clothing features distinct physical layers: inner collared shirt, silk tie/cravat, double-breasted uniform waistcoat/tunic with brass button accents, tailored trousers, and polished leather shoes.

### 5.2 Representative Character: Concierge Pierre
- **Identity:** Head Concierge Pierre Dubois — impeccably composed, observant, formal, carrying a hidden burden of complicity.
- **Silhouette:** Upright, formal posture. Tailored navy blue hotel livery jacket with gold-braided epaulets and polished brass buttons, burgundy pocket square, formal charcoal slacks, polished black Oxford shoes.
- **Model Construction:** Multi-material 3D stylized human geometry with clean anatomical loops, distinct head and hair sculpt, bespoke livery uniform, hands, and facial features.
- **Simulation Compatibility:** Fully integrated with `NavMeshAgent`, `NPCController`, sensory perception raycasts, and interaction colliders.

---

## 6. Meridian House: Hotel Environment & Architectural Identity

### 6.1 Architectural Language
The lobby is structured as an authentic luxury hotel interior:
- **Floor:** Diamond-checker Italian marble tiles (polished cream ivory and deep forest green / obsidian marble) bordered by warm dark mahogany trim and a plush burgundy runner rug.
- **Walls & Wainscoting:** Lower half features dark fluted mahogany wood wainscoting with decorative baseboards; upper half features Art Deco damask wallpaper in muted emerald/olive with subtle gold foil line motifs, crowned with stepped plaster cornices.
- **Ceiling:** Dark coffered wood beam ceiling with recessed warm ambient uplights and a central brass chandelier.
- **Columns:** Fluted square pillars with carved mahogany bases and Corinthian-style brass capitals.
- **Reception Desk:** Curved/fluted mahogany front, polished black granite countertop, brass key-slot cubbies, guest ledger, and a classic brass banker's desk lamp with green glass shade.
- **Room 102 Entrance:** Substantial paneled mahogany double door with brass lever handles, brass kickplate, ornamental transoms, and a glowing electronic card reader plate.
- **Center Lobby Table:** Circular burled walnut table with brass cabriole legs holding the Room 102 Keycard.

### 6.2 Lighting Design
- **Key Atmosphere:** Midnight mystery, 12:00 AM.
- **Primary Practicals:**
  - Center Brass Chandelier: Warm 2700K point/spot illumination casting soft circular pools of light on the center floor.
  - Wall Sconces: Pair of art-deco brass and frosted glass sconces flanking Room 102.
  - Banker's Lamp: Warm focused green-tinted glow on the reception desk.
- **Ambient Fill:** Deep indigo/peacock fill light (0.25 intensity) simulating ambient midnight moonlight filtering through high clerestory windows, establishing dramatic chiaroscuro with the warm interior lamps.

---

## 7. Rebuilt User Interface & Graphic Design System

Every screen is rebuilt using the **Case-File Dossier** visual language:

### 7.1 Typography System
- **Display / Title:** Classic High-Contrast Serif (`Times New Roman` / `Georgia` / stylized serif typography) for titles, section headers, and clue names.
- **Body / Narrative:** Clean, highly legible Modern Sans-Serif for clue descriptions, observation notes, and settings.
- **Data / Timestamps:** Monospace / Classic Numerals for simulation clock (`12:00:00`), coordinates, and loop counters.

### 7.2 Screen-by-Screen Specification
1. **Title / Main Menu:**
   - Cinematic composition: Meridian House gold-embossed seal, elegant serif title, dramatic subtle vignette, and refined primary/secondary action buttons with polished brass borders and hover illumination.
2. **In-Game HUD:**
   - **Loop Clock:** Ornate art-deco pocket-watch inspired casing with brass rim, remaining time readout, and normalized loop bar with dynamic urgency color transition.
   - **Interaction Reticle:** Dynamic diamond focus reticle that expands into a gold-framed action badge when gazing at interactive objects.
   - **Toasts:** Dispatch card notifications with brass left-accent borders, category stamps, and smooth fade transitions.
   - **Dossier Pill:** Lower-right gold-edged pill displaying real-time clue count and observation status.
3. **Investigation Notebook (Workspace 2.0):**
   - Presented as an authentic leather-bound Master Case Dossier with embossed gold lettering and five dedicated tabs:
     - **Evidence Library:** Case cards with category stamps ("PHYSICAL", "DOCUMENT", "LOCATION"), detailed examination viewport, and discovery stamps.
     - **Timeline & Loop Comparison:** Chronological investigation chronometer with loop tracks and anomaly diff indicators.
     - **Deduction Board:** Cork/felt investigation board with brass pins, clue cards, and typed semantic thread connections (`Supports`, `Contradicts`, `RelatedTo`, `OccurredBefore`, `OccurredAfter`).
     - **Hypotheses Manager:** Official deduction case sheets with evidence check-markers.
     - **Field Notes:** Persistent investigator scratchpad.
4. **Evidence Inspection Modal:**
   - Dedicated illuminated examination view showing the physical clue on a velvet display plinth with an archival case card, high-contrast narrative text, discovery timestamp, and linked causal events.
5. **Pause & Settings Menus:**
   - Cohesive luxury styling with brass sliders, toggle buttons, and responsive controls matching the game's aesthetic.

---

## 8. Asset Requirements & Replacement Matrix

```
+-----------------------------------+-----------------------------------+-----------------------------------+
| Existing Asset / System           | Current State (Failed Experiment) | New Rebuilt Implementation        |
+-----------------------------------+-----------------------------------+-----------------------------------+
| Concierge Character               | Primitive blue Capsule            | 3D Stylized Livery Uniform Model  |
| Lobby Floor                       | Flat charcoal primitive Cube      | Checker Marble & Mahogany Runner  |
| Lobby Walls                       | Gray primitive Cubes              | Wainscoting + Art Deco Wallpaper  |
| Columns & Pillars                 | Gray primitive Cubes              | Fluted Columns with Brass Trim    |
| Reception Desk                    | Brown primitive Cube              | Curved Mahogany & Granite Counter |
| Lobby Table                       | Brown primitive Cube              | Round Burled Walnut Center Table  |
| Room 102 Door                     | Brown primitive Cube              | Paneled Mahogany with Brass Lever |
| Keycard Prop                      | Yellow primitive Cube             | Gold-Embossed Meridian Keycard    |
| Schedule Memo Prop                | White primitive Cube              | Linen Hotel Stationery Document   |
| Card Reader Prop                  | Dark gray primitive Cube          | Polished Brass Electronic Reader  |
| Lighting Composition              | 1 default light + 1 point light   | Multi-source Chiaroscuro Rig      |
| Main Menu                         | Flat charcoal IMGUI box           | Theatrical Gold-Embossed Title UI |
| In-Game HUD                       | Basic debug-like boxes            | Art-Deco Pocket Chronometer HUD   |
| Investigation Notebook            | Flat gray 3-column IMGUI layout   | Leather Case Dossier & Board      |
| Evidence Inspection Modal         | Flat gray box                     | Velvet Plinth & Archival Card     |
| Color System                      | Monotone gray & charcoal          | Coordinated Jewel-Tone Palette    |
+-----------------------------------+-----------------------------------+-----------------------------------+
```

---

## 9. Implementation Plan & Quality Gates

### Phase A: Architecture & Visual Foundation (Current)
- Document reference analysis and art direction in `Docs/ART_DIRECTION.md`.
- Build centralized `MeridianTheme` design tokens (colors, procedural textures, borders, cards, badges, buttons, typography).

### Phase B: Vertical Slice Implementation
- Implement the 3D Stylized Concierge Character Model with distinct uniform materials, silhouette, and facial structure.
- Rebuild the 3D Hotel Lobby environment with architectural wainscoting, checkered marble, fluted columns, mahogany furniture, and brass hardware.
- Implement multi-source theatrical lighting rig with warm practicals and cool ambient fill.
- Rebuild all UI views (Main Menu, HUD, Notebook, Evidence Modal, Pause, Settings) using the new Art Deco / Case Dossier design language.

### Phase C: System Integration & Regression Testing
- Verify all 71 existing EditMode tests pass without regression.
- Verify simulation clock, loop reset, player locomotion/interaction, NPC schedules, causal events, and persistent knowledge remain 100% operational.
- Capture and inspect actual runtime screenshots in Unity.

---

## 10. Visual Acceptance Criteria

1. **Distinct Character Silhouette:** The Concierge NPC is recognizably human, stylized, wearing a formal hotel uniform with layered materials, and is visibly distinguishable from any primitive capsule.
2. **Architectural Personality:** The hotel lobby features distinct wood wainscoting, marble flooring, columns, and bespoke furniture rather than untextured primitive cubes.
3. **Theatrical Lighting:** The scene exhibits warm practical lighting (chandeliers, lamps) contrasting against midnight blue shadows, providing both mood and high clue visibility.
4. **Cohesive Design System:** The Main Menu, HUD, Notebook, and Modals share identical jewel tones, brass trim, serif titles, and case-dossier cards.
5. **Zero Regression:** All existing player controls, 5-minute loop countdown, resets, NPC schedules, and causal event triggers work flawlessly.
6. **Verified Visual Quality:** Screenshots captured from the actual running Unity scene demonstrate marked, undeniable visual improvement.
