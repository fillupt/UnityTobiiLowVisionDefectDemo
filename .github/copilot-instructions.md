# Unity Tobii Low Vision Defect Demo - AI Coding Agent Instructions

## Project Overview
This is a Unity 2022.3.62f3 educational application that simulates visual impairments (glaucoma, AMD, cataracts, oedema) using Tobii 4C eye-tracking hardware. The shader-based effects follow the user's gaze to demonstrate how vision loss affects daily activities.

## Architecture

### Core Components
- **GetGaze.cs** - Central controller managing Tobii API integration, simulation state, and shader parameters
- **ChangePicture.cs** - UI/input handler for image switching and scene lifecycle
- **DistortionShader.shader** - Custom surface shader applying visual impairment effects with circular vignettes and distortion
- **StartMouseMode.cs** - Fallback mode using mouse position when Tobii hardware unavailable

### Data Flow
1. Tobii API provides gaze coordinates → `GetGaze.Update()` converts to normalized screen space
2. Keyboard input (G/A/C/O) selects simulation type → sets shader parameters in `GetGaze`
3. Arrow keys modify severity → adjusts `vigSize`, `vigAlpha`, `distortRadius` based on `currSim` state
4. `ApplyDistortion()` pushes parameters to material → shader renders effect centered on gaze/mouse

## Key Patterns & Conventions

### Simulation State Machine
Each vision defect has distinct shader parameter profiles set in `GetGaze.Update()`:
- **Glaucoma (G)**: Peripheral vision loss - `vigInvert=false`, grows `vigSize` with severity
- **AMD (A)**: Central vision loss - `vigInvert=true`, small `vigSize` (0.05-0.7), random wetness distortion
- **Cataract (C)**: Clouding - `catColour` overlay, large `vigSize` (0.7-1.0), controlled opacity fade
- **Oedema (O)**: Distortion - radius-based distortion with exponential size calculation

When modifying simulations, preserve the inversion logic and parameter ranges to maintain medical accuracy.

### Tobii Integration
- Check `TobiiAPI.IsConnected` before accessing eye-tracking
- Mouse fallback: Right-click simulates "user not present", left position = gaze substitute
- Timeout logic: `inactiveTimeOut` (1.5s) gradually disables shader when user not detected
- Never remove mouse mode - it's essential for testing without hardware

### UI Keyboard Controls
- Number keys (1-0, -, =): Switch images from `theIms` array
- Backtick (`): Play video content
- G/A/C/O + corresponding toggle enabled: Activate simulation
- R: Reset shader to neutral state
- Up/Down arrows: Increase/decrease severity
- Escape: Reload scene or quit (context-dependent on `SOVSlogo.enabled`)

## Development Workflows

### Testing Without Tobii Hardware
1. Start application → automatically falls back to mouse mode
2. Or enable "Mouse Mode" toggle → hides instructions, resizes UI
3. Right-click to simulate user absence and test timeout behavior

### Adding New Simulation Types
1. Add toggle to scene (reference `aT`, `cT`, `gT`, `oT` pattern)
2. Add key check in `GetGaze.Update()` (e.g., `if (Input.GetKeyUp(KeyCode.X) && xT.isOn)`)
3. Set `currSim` to new int value, define shader parameters (`vigColor`, `vigSize`, `distortionAmount`, etc.)
4. Add severity adjustment logic in Up/Down arrow blocks using `Mathf.Clamp` for safety
5. Update README.md with new keybinding

### Modifying Shader Effects
- Material parameters set via `material.SetFloat/SetVector/SetColor` in `ApplyDistortion()`
- Shader properties defined in `DistortionShader.shader` Properties block
- `_EnableShader` float gates entire effect (0=passthrough)
- Distortion uses Gaussian blur fade: `exp(-(distance * distance) / (2.0 * _BlurStrength * _BlurStrength))`
- Vignette respects aspect ratio via `_ScreenParams` normalization

### Building the Application
- Unity target: Standalone Windows (companyName: "fillupt", productName: "LV Simulator")
- Build via Unity Editor: File → Build Settings → Build
- Solution file exists for IDE integration but Unity manages compilation

## Critical Dependencies
- **Tobii Gaming SDK**: `Assets/Tobii/` contains native plugin (`tobii_gameintegration_x64.dll`)
- **TextMesh Pro**: Used for UI text (statusText, instructions)
- **Video Player**: Plays demonstration content on backtick key

## Common Pitfalls
- Don't modify shader center directly - always update via `shaderCentre` in `GetGaze` (handles coordinate space conversion)
- Severity changes use `Time.deltaTime` for frame-rate independence - preserve this pattern
- `RandomlyFlip.cs` animates logo - uses coroutine pattern, don't convert to Update loop
- Scene 0 is the only scene - `SceneManager.LoadScene(0)` is intentional reload, not multi-scene navigation
- `hasStarted` flag prevents input before user clicks "Begin" button - check this in any new input handlers

## File Organization
- All scripts in `Assets/` root (flat structure by design)
- Shader and material paired: `DistortionShader.shader` + `DistortionMat.mat`
- Single scene: `Assets/Scenes/DesktopTrackerScene.unity`
- Media assets: `Assets/images/`, `Assets/movie/`, `Assets/logo/`
