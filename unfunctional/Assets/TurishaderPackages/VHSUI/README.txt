VHS UI EFFECT – README

Overview
VHS UI EFFECT is a stylized shader pack that brings analog VHS aesthetics to Unity UI. Designed for both URP and Built-in pipelines, this package includes support for TextMeshPro, CanvasGroup, UI tint, animated glitch, scanlines, and mipmap-based blur.

Requirements
- Unity Version: 2022.3.14 or newer
- Render Pipeline: Universal Render Pipeline (URP) or Built-in
- TextMeshPro Version: 3.0.9
- Textures must have mipmaps enabled for the blur effect to work

Installation
1. Import the package into your Unity project.
2. Drag the included VHS UI materials to your UI elements or assign them in your CanvasRenderer / TextMeshPro component.
3. Adjust material properties as needed.

Usage
Use the provided shaders to achieve a retro VHS look on UI images and text elements.
- Use VHSUI.shader for images, raw images, or overlays.
- Use VHSUIText.shader for TextMeshPro components.
Both shaders support runtime manipulation via script or animator.

Material Properties Explained
Color
- ColorMultiplier: Controls the brightness/intensity of the base color.
- Color bleeding amount: Shifts the color channels slightly to simulate chromatic bleeding.

Mip level blur (requires mipmaps on texture)
- MipLevel: Controls which mipmap level is sampled. Higher values result in more blur.
- BlurAmount: Intensity multiplier for the mip level blur. Use with MipLevel for analog distortion.

Grid
- PixelGridAmount: Number of grid cells used to simulate pixelation.
- PixelGridSize: Size of each pixel block. Lower values = higher pixelation.

Scanlines
- ScanLinesAmount: Controls the opacity of the scanlines.
- ScanLinesFrequency: How many scanlines are shown across the vertical axis.
- ScanLinesSpeed: Animation speed for the scanlines (scrolling effect).

Noise
- NoiseAmount: Adds dynamic flickering noise to the image to simulate VHS instability.

Glitch
- GlitchAmount: Intensity of the glitching/shifting effect.
- GlitchTiling: Frequency/scale of the glitch pattern.
- GlitchMinOpacity: Minimum opacity of the glitch flicker overlay.
- GlitchMinColorMultiply: Controls how much the glitch affects color values.
- DistortionAmount: Warping/distortion intensity during glitch spikes.

Tips
- Use CanvasGroup alpha to fade UI in/out while keeping effects active.
- Combine scanlines, blur, and glitch for maximum analog fidelity.
- You can keyframe GlitchAmount and DistortionAmount to simulate signal drops or spikes.

Support
If you encounter any issues or need help integrating the effect, feel free to contact us through the Unity Asset Store support page or writing to turishader@gmail.com
