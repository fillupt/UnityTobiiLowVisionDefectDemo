# Unity Tobii Low Vision Defect Demo - AI Coding Agent Instructions

## Project Overview
This is a Unity 2022.3.62f3 educational application that simulates visual impairments (glaucoma, AMD, cataracts, oedema) using Tobii 4C eye-tracking hardware. The shader-based effects follow the user's gaze to demonstrate how vision loss affects daily activities.

## Architecture

### Core Components
- **GetGaze.cs** - Central controller managing Tobii API integration, simulation state, and shader parameters across 4 separate materials
- **ChangePicture.cs** - UI/input handler for image switching and scene lifecycle
- **GlaucomaShader.shader** - Unlit shader with peripheral vision loss and gradient darkening (grey to black)
- **AMDShader.shader** - Unlit shader with central scotoma, irregular boundaries, Perlin noise color variation, and distortion
- **CataractShader.shader** - Unlit shader with clouding overlay, irregular boundaries, and Perlin noise for mottled appearance
- **OedemaShader.shader** - Unlit shader with localized distortion from fluid accumulation
- **StartMouseMode.cs** - Fallback mode using mouse position when Tobii hardware unavailable

### Data Flow
1. Tobii API provides gaze coordinates → `GetGaze.Update()` converts to normalized screen space
2. Keyboard input (G/A/C/O) selects simulation type → switches active material and sets shader parameters in `GetGaze`
3. Arrow keys modify severity → adjusts condition-specific parameters (`vigSize`, `vigAlpha`, `distortRadius`, `scotomaIrregularity`, `cataractIrregularity`) based on `currSim` state
4. `ApplyShaderParameters()` pushes parameters to active material → shader renders effect centered on gaze/mouse

## Key Patterns & Conventions

### Simulation State Machine
Each vision defect uses a dedicated shader with condition-specific parameters set in `GetGaze.Update()`:
- **Glaucoma (G, currSim=0)**: Uses `GlaucomaShader` - Peripheral vision loss with gradient darkening, grows `vigSize` (0.05-0.8) with severity
- **AMD (A, currSim=1)**: Uses `AMDShader` - Central scotoma with irregular boundaries (multi-frequency sine waves), Perlin noise color variation (±2%), distortion scales with severity (0.3-1.0), `scotomaIrregularity` (0.1-0.8)
- **Cataract (C, currSim=2)**: Uses `CataractShader` - Central clouding overlay with irregular boundaries, Perlin noise for mottled appearance (±5%), `vigSize` (0.7-1.0), `cataractIrregularity` (0.15-0.7)
- **Oedema (O, currSim=4)**: Uses `OedemaShader` - Localized distortion with Gaussian blur fade, radius-based distortion with exponential size calculation

When modifying simulations, preserve parameter ranges and irregularity patterns to maintain medical accuracy. Each shader implements Perlin noise via hash function + bilinear interpolation + FBM (3 octaves) for organic variation.

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
1. Create new unlit shader in `Assets/` with appropriate visual effect (reference existing shaders)
2. Create corresponding material and assign shader to it
3. Add material reference in `GetGaze.cs` (e.g., `public Material newConditionMaterial;`)
4. Add toggle to scene (reference `aT`, `cT`, `gT`, `oT` pattern)
5. Add key check in `GetGaze.Update()` with material switch: `image.material = newConditionMaterial;`
6. Set `currSim` to new int value, initialize condition-specific parameters
7. Add severity adjustment logic in Up/Down arrow blocks using `Mathf.Clamp` for safety
8. Add parameter passing in `ApplyShaderParameters()` method
9. Update README.md with new keybinding

### Modifying Shader Effects
- Each condition has its own shader file and material - prefer separate shaders over conditional branching
- Material parameters set via `mat.SetFloat/SetVector/SetColor` in `ApplyShaderParameters()`
- All shaders are **unlit** (converted from surface shaders to prevent darkening)
- `_EnableShader` float gates entire effect (0=passthrough)
- Distortion uses Gaussian blur fade: `exp(-(distance * distance) / (2.0 * _BlurStrength * _BlurStrength))`
- Vignette respects aspect ratio via `aspectRatio = _ScreenParams.x / _ScreenParams.y` applied to x-coordinate
- **Perlin Noise Pattern**: For organic variation, use hash function → bilinear interpolation noise → FBM with 3 octaves (see AMDShader/CataractShader)
- **Irregular Boundaries**: Use multi-frequency sine waves on angle (e.g., `sin(angle * 3.0) + sin(angle * 5.0)`) combined with FBM
- Coordinate space: `_GazeCenter` in normalized UV (0-1), shaders calculate distance from gaze for masking/distortion

### Building the Application
- Unity target: Standalone Windows (companyName: "fillupt", productName: "LV Simulator")
- Build via Unity Editor: File → Build Settings → Build
- Solution file exists for IDE integration but Unity manages compilation

## Critical Dependencies
- **Tobii Gaming SDK**: `Assets/Tobii/` contains native plugin (`tobii_gameintegration_x64.dll`)
- **TextMesh Pro**: Used for UI text (statusText, instructions)
- **Video Player**: Plays demonstration content on backtick key

## Common Pitfalls
- Don't modify shader center directly - always update via `shaderCentre` in `GetGaze` (handles coordinate space conversion and aspect ratio)
- Severity changes use `Time.deltaTime` for frame-rate independence - preserve this pattern
- When adding irregularity parameters, always initialize them in the KeyCode check AND adjust in up/down arrow severity controls
- Use separate materials per condition - don't try to combine all effects into one shader (performance and maintainability)
- Aspect ratio correction: multiply x-coordinate by `aspectRatio`, not y-coordinate (fixes horizontal stretching)
- Perlin noise UV coordinates must move with gaze center for proper scotoma tracking
- `RandomlyFlip.cs` animates logo - uses coroutine pattern, don't convert to Update loop
- Scene 0 is the only scene - `SceneManager.LoadScene(0)` is intentional reload, not multi-scene navigation
- `hasStarted` flag prevents input before user clicks "Begin" button - check this in any new input handlers

## File Organization
- All scripts in `Assets/Scripts/` directory
- Four condition-specific shader+material pairs:
  - `GlaucomaShader.shader` + `GlaucomaMat.mat`
  - `AMDShader.shader` + `AMDMat.mat`
  - `CataractShader.shader` + `CataractMat.mat`
  - `OedemaShader.shader` + `OedemaMat.mat`
- Single scene: `Assets/Scenes/DesktopTrackerScene.unity`
- Media assets: `Assets/images/`, `Assets/movie/`, `Assets/logo/`
- Legacy files: `DistortionShader.shader` and `DistortionMat.mat` (deprecated, may be removed)
