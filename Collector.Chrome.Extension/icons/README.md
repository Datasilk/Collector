# Extension Icons

Place the following icon files in this folder:
- icon16.png (16x16 pixels)
- icon48.png (48x48 pixels)
- icon128.png (128x128 pixels)

These icons will be used for the extension in Chrome's toolbar and extension management page.

## Generate Icons from SVG

You can use the included `icon.svg` to generate PNG icons:

### Install ImageMagick (Windows):
```powershell
# Using winget (Windows Package Manager):
winget install ImageMagick.ImageMagick

# Or using Chocolatey:
choco install imagemagick

# Or using Scoop:
scoop install imagemagick
```

After installing, restart your terminal, then run:
```bash
magick icon.svg -resize 16x16 icon16.png
magick icon.svg -resize 48x48 icon48.png
magick icon.svg -resize 128x128 icon128.png
```

### Using online tools:
1. Go to https://cloudconvert.com/svg-to-png
2. Upload icon.svg
3. Set output size and download
