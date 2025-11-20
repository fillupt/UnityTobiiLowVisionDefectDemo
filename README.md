# Unity Tobii Low Vision Defect Demo

An educational application demonstrating the effects of common visual impairments using eye tracking technology. Built with Unity and Tobii eye tracking. Does not collect any user data, only applies at runtime. 

## Features

Simulates four types of vision loss:
- **Glaucoma** - Peripheral vision loss
- **Age-Related Macular Degeneration (AMD)** - Central vision loss
- **Cataract** - Clouding and reduced contrast
- **Oedema** - Visual distortion

The shader effects follow your gaze in real-time, providing an immersive experience of how these conditions affect daily activities. If an eye tracking is unavailable, it defaults to 'mouse' mode to simulate gaze. 

## Controls

- **Number keys (1-0, -, =)** - Switch between different images
- **Left/Right arrows** - Navigate through images
- **Backtick (`)** - Play video demonstration
- **G, A, C, O** - Activate Glaucoma, AMD, Cataract, or Oedema simulation (toggle must be enabled)
- **Up/Down arrows** - Increase/decrease severity of active simulation
- **R** - Reset shader to neutral state
- **Escape** - Return to menu or quit application

## Requirements

- Windows PC
- Tobii 4C eye tracker (optional - mouse mode available for testing)
- Unity 2022.3.62f3 or later (to build, otherwise just use compiled version)

## Author

**Dr Philip Turnbull**  
Virtualeyes Lab  
School of Optometry and Vision Science  
University of Auckland
p.turnbull@auckland.ac.nz

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.
