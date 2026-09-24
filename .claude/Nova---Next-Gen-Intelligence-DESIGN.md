---
version: "alpha"
name: "Nova - Next Gen Intelligence"
description: "Nova Next Feature Section is designed for highlighting product capabilities and value points. Key features include reusable structure, responsive behavior, and production-ready presentation. It is suitable for component libraries and responsive product interfaces."
colors:
  primary: "#9CA3AF"
  secondary: "#FFFFFF"
  tertiary: "#A69DB9"
  neutral: "#FFFFFF"
  background: "#FFFFFF"
  surface: "#FFFFFF"
  text-primary: "#FFFFFF"
  text-secondary: "#D1D5DB"
  border: "#FFFFFF"
  accent: "#9CA3AF"
typography:
  display-lg:
    fontFamily: "Geist"
    fontSize: "88px"
    fontWeight: 300
    lineHeight: "88px"
    letterSpacing: "-0.05em"
  body-md:
    fontFamily: "Geist"
    fontSize: "18px"
    fontWeight: 500
    lineHeight: "28px"
  label-md:
    fontFamily: "Geist"
    fontSize: "14px"
    fontWeight: 500
    lineHeight: "20px"
rounded:
  md: "0px"
  full: "9999px"
spacing:
  base: "4px"
  sm: "4px"
  md: "8px"
  lg: "12px"
  xl: "14px"
  gap: "8px"
  section-padding: "112px"
components:
  button-primary:
    backgroundColor: "{colors.secondary}"
    textColor: "#000000"
    typography: "{typography.label-md}"
    rounded: "{rounded.full}"
    padding: "14px"
  button-secondary:
    textColor: "{colors.secondary}"
    typography: "{typography.label-md}"
    rounded: "{rounded.full}"
    padding: "8px"
  button-link:
    textColor: "{colors.text-secondary}"
    typography: "{typography.label-md}"
    rounded: "{rounded.md}"
    padding: "0px"
---

## Overview

- **Composition cues:**
  - Layout: Flex
  - Content Width: Bounded
  - Framing: Glassy
  - Grid: Minimal

## Colors

The color system uses dark mode with #9CA3AF as the main accent and #FFFFFF as the neutral foundation.

- **Primary (#9CA3AF):** Main accent and emphasis color.
- **Secondary (#FFFFFF):** Supporting accent for secondary emphasis.
- **Tertiary (#A69DB9):** Reserved accent for supporting contrast moments.
- **Neutral (#FFFFFF):** Neutral foundation for backgrounds, surfaces, and supporting chrome.

- **Usage:** Background: #FFFFFF; Surface: #FFFFFF; Text Primary: #FFFFFF; Text Secondary: #D1D5DB; Border: #FFFFFF; Accent: #9CA3AF

- **Gradients:** bg-gradient-to-br from-[#bef264] to-[#60a5fa] via-[#34d399]

## Typography

Typography relies on Geist across display, body, and utility text.

- **Display (`display-lg`):** Geist, 88px, weight 300, line-height 88px, letter-spacing -0.05em.
- **Body (`body-md`):** Geist, 18px, weight 500, line-height 28px.
- **Labels (`label-md`):** Geist, 14px, weight 500, line-height 20px.

## Layout

Layout follows a flex composition with reusable spacing tokens. Preserve the flex, bounded structural frame before changing ornament or component styling. Use 4px as the base rhythm and let larger gaps step up from that cadence instead of introducing unrelated spacing values.

Treat the page as a flex / bounded composition, and keep that framing stable when adding or remixing sections.

- **Layout type:** Flex
- **Content width:** Bounded
- **Base unit:** 4px
- **Scale:** 4px, 8px, 12px, 14px, 16px, 20px, 24px, 32px
- **Section padding:** 112px
- **Gaps:** 8px, 12px, 16px, 20px

## Elevation & Depth

Depth is communicated through glass, border contrast, and reusable shadow or blur treatments. Keep those recipes consistent across hero panels, cards, and controls so the page reads as one material system.

Surfaces should read as glass first, with borders, shadows, and blur only reinforcing that material choice.

- **Surface style:** Glass
- **Borders:** 1px #FFFFFF
- **Blur:** 16px

### Techniques
- **Gradient border shell:** Use a thin gradient border shell around the main card. Wrap the surface in an outer shell with 2px padding and a 0px radius. Drive the shell with none so the edge reads like premium depth instead of a flat stroke. Keep the actual stroke understated so the gradient shell remains the hero edge treatment. Inset the real content surface inside the wrapper with a slightly smaller radius so the gradient only appears as a hairline frame.

## Shapes

Shapes rely on a tight radius system anchored by 9999px and scaled across cards, buttons, and supporting surfaces. Icon geometry should stay compatible with that soft-to-controlled silhouette.

Use the radius family intentionally: larger surfaces can open up, but controls and badges should stay within the same rounded DNA instead of inventing sharper or pill-only exceptions.

- **Corner radii:** 9999px
- **Icon treatment:** Linear
- **Icon sets:** Solar

## Components

Anchor interactions to the detected button styles.

### Buttons
- **Primary:** background #FFFFFF, text #000000, radius 9999px, padding 14px, border 0px solid rgb(229, 231, 235).
- **Secondary:** text #FFFFFF, radius 9999px, padding 8px, border 1px solid rgba(255, 255, 255, 0.2).
- **Links:** text #D1D5DB, radius 0px, padding 0px, border 0px solid rgb(229, 231, 235).

### Iconography
- **Treatment:** Linear.
- **Sets:** Solar.

## Do's and Don'ts

Use these constraints to keep future generations aligned with the current system instead of drifting into adjacent styles.

### Do
- Do use the primary palette as the main accent for emphasis and action states.
- Do keep spacing aligned to the detected 4px rhythm.
- Do reuse the Glass surface treatment consistently across cards and controls.
- Do keep corner radii within the detected 9999px family.

### Don't
- Don't introduce extra accent colors outside the core palette roles unless the page needs a new semantic state.
- Don't mix unrelated shadow or blur recipes that break the current depth system.
- Don't exceed the detected moderate motion intensity without a deliberate reason.

## Motion

Motion feels controlled and interface-led across text, layout, and section transitions. Timing clusters around 200ms and 300ms. Easing favors ease and cubic-bezier(0.4. Hover behavior focuses on text and color changes. Scroll choreography uses GSAP ScrollTrigger for section reveals and pacing.

**Motion Level:** moderate

**Durations:** 200ms, 300ms, 150ms

**Easings:** ease, cubic-bezier(0.4, 0, 0.2, 1)

**Hover Patterns:** text, color, transform

**Scroll Patterns:** gsap-scrolltrigger

## WebGL

Reconstruct the graphics as a full-bleed background field using webgl, renderer, alpha, antialias, dpr clamp, custom shaders. The effect should read as technical and meditative: fine line lattice with black and sparse spacing. Build it from line trails + sparse anchors so the effect reads clearly. Animate it as slow breathing pulse. Interaction can react to the pointer, but only as a subtle drift. Preserve dom fallback.

**Id:** webgl

**Label:** WebGL

**Stack:** ThreeJS, WebGL

**Insights:**
  - **Scene:**
    - **Value:** Full-bleed background field
  - **Effect:**
    - **Value:** Fine line lattice
  - **Primitives:**
    - **Value:** Line trails + sparse anchors
  - **Motion:**
    - **Value:** Slow breathing pulse
  - **Interaction:**
    - **Value:** Pointer-reactive drift
  - **Render:**
    - **Value:** WebGL, Renderer, alpha, antialias, DPR clamp, custom shaders

**Techniques:** Perspective grid, Line lattice, Breathing pulse, Pointer parallax, Shader gradients

**Code Evidence:**
  - **HTML reference:**
    - **Language:** html
    - **Snippet:**
      ```html
      <!-- WebGL Canvas Container -->
      <canvas id="webgl-bg"></canvas>

      <!-- Main Content Wrapper (Above WebGL) -->
      ```
  - **JS reference:**
    - **Language:** js
    - **Snippet:**
      ```
      // --- WebGL Background Animation ---
      const initWebGL = () => {
          const canvas = document.getElementById('webgl-bg');
          const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
          const scene = new THREE.Scene();
          const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);

          // Custom fragment shader for diagonal glowing rays
      …
      ```
  - **Renderer setup:**
    - **Language:** js
    - **Snippet:**
      ```
      // --- WebGL Background Animation ---
      const initWebGL = () => {
          const canvas = document.getElementById('webgl-bg');
          const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
          const scene = new THREE.Scene();
          const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);

          // Custom fragment shader for diagonal glowing rays
      ```
  - **Draw call:**
    - **Language:** js
    - **Snippet:**
      ```
      const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);

      // Custom fragment shader for diagonal glowing rays
      const fragmentShader = `
          uniform float u_time;
          uniform vec2 u_resolution;

          void main() {
      …
      ```

## ThreeJS

Reconstruct the Three.js layer as a full-bleed background field with layered spatial depth that feels technical. Use alpha, antialias, dpr clamp renderer settings, orthographic projection, plane geometry, shadermaterial materials, and ambient + key + rim lighting. Motion should read as slow orbital drift, with poster frame + dom fallback.

**Id:** threejs

**Label:** ThreeJS

**Stack:** ThreeJS, WebGL

**Insights:**
  - **Scene:**
    - **Value:** Full-bleed background field with layered spatial depth
  - **Render:**
    - **Value:** alpha, antialias, DPR clamp
  - **Camera:**
    - **Value:** Orthographic projection
  - **Lighting:**
    - **Value:** ambient + key + rim
  - **Materials:**
    - **Value:** ShaderMaterial
  - **Geometry:**
    - **Value:** plane
  - **Motion:**
    - **Value:** Slow orbital drift

**Techniques:** Shader materials, Timeline beats, alpha, antialias, DPR clamp, Poster frame + DOM fallback

**Code Evidence:**
  - **HTML reference:**
    - **Language:** html
    - **Snippet:**
      ```html
      <!-- WebGL Canvas Container -->
      <canvas id="webgl-bg"></canvas>

      <!-- Main Content Wrapper (Above WebGL) -->
      ```
  - **JS reference:**
    - **Language:** js
    - **Snippet:**
      ```
      // --- WebGL Background Animation ---
      const initWebGL = () => {
          const canvas = document.getElementById('webgl-bg');
          const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
          const scene = new THREE.Scene();
          const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);

          // Custom fragment shader for diagonal glowing rays
      …
      ```
  - **Renderer setup:**
    - **Language:** js
    - **Snippet:**
      ```
      // --- WebGL Background Animation ---
      const initWebGL = () => {
          const canvas = document.getElementById('webgl-bg');
          const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
          const scene = new THREE.Scene();
          const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);

          // Custom fragment shader for diagonal glowing rays
      ```
  - **Draw call:**
    - **Language:** js
    - **Snippet:**
      ```
      const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);

      // Custom fragment shader for diagonal glowing rays
      const fragmentShader = `
          uniform float u_time;
          uniform vec2 u_resolution;

          void main() {
      …
      ```