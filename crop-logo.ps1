# COFFEEBOTIC Logo Cropper
# This script extracts the circular logo from the palette image

Add-Type -AssemblyName System.Drawing

# Load the source image
$sourceImage = [System.Drawing.Image]::FromFile("$PSScriptRoot\Assets\palette.jpg")

# Define crop area for the circular logo
# Based on the palette.jpg layout: logo is in the lower-left portion
$cropX = 50
$cropY = 370
$cropWidth = 400
$cropHeight = 400

# Create a new bitmap for the cropped area
$croppedBitmap = New-Object System.Drawing.Bitmap($cropWidth, $cropHeight)
$graphics = [System.Drawing.Graphics]::FromImage($croppedBitmap)

# Set high quality rendering
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

# Define source and destination rectangles
$srcRect = New-Object System.Drawing.Rectangle($cropX, $cropY, $cropWidth, $cropHeight)
$destRect = New-Object System.Drawing.Rectangle(0, 0, $cropWidth, $cropHeight)

# Draw the cropped portion
$graphics.DrawImage($sourceImage, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

# Save as PNG with transparency support
$outputPath = "$PSScriptRoot\Assets\Branding\logo.png"
$croppedBitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Cleanup
$graphics.Dispose()
$croppedBitmap.Dispose()
$sourceImage.Dispose()

Write-Host "✓ Logo extracted successfully!" -ForegroundColor Green
Write-Host "  Saved to: Assets\Branding\logo.png" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now run the application with the COFFEEBOTIC logo!" -ForegroundColor Yellow
