# Remove background from COFFEEBOTIC logo
# Converts beige background to transparent

Add-Type -AssemblyName System.Drawing

# Load the source image
$sourceImage = [System.Drawing.Bitmap]::FromFile("$PSScriptRoot\Assets\Branding\coffeebotic.jpg")

# Create a new bitmap with alpha channel for transparency
$width = $sourceImage.Width
$height = $sourceImage.Height
$transparentBitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

# Define the background color to remove (beige: approximately #D8CABAB or similar)
# We'll use a tolerance to catch slight variations
$bgColorMin = [System.Drawing.Color]::FromArgb(200, 180, 160)  # Lower bound
$bgColorMax = [System.Drawing.Color]::FromArgb(240, 220, 200)  # Upper bound

# Process each pixel
for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $pixel = $sourceImage.GetPixel($x, $y)

        # Check if pixel is within beige background range
        $isBackground = ($pixel.R -ge $bgColorMin.R -and $pixel.R -le $bgColorMax.R) -and
                       ($pixel.G -ge $bgColorMin.G -and $pixel.G -le $bgColorMax.G) -and
                       ($pixel.B -ge $bgColorMin.B -and $pixel.B -le $bgColorMax.B)

        if ($isBackground) {
            # Make transparent
            $transparentBitmap.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
        } else {
            # Keep original color
            $transparentBitmap.SetPixel($x, $y, $pixel)
        }
    }

    # Progress indicator
    if ($y % 50 -eq 0) {
        $percent = [math]::Round(($y / $height) * 100)
        Write-Host "`rProcessing: $percent%" -NoNewline
    }
}

Write-Host "`rProcessing: 100%"

# Save as PNG with transparency
$outputPath = "$PSScriptRoot\Assets\Branding\coffeebotic-transparent.png"
$transparentBitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Cleanup
$transparentBitmap.Dispose()
$sourceImage.Dispose()

Write-Host ""
Write-Host "✓ Background removed successfully!" -ForegroundColor Green
Write-Host "  Saved to: Assets\Branding\coffeebotic-transparent.png" -ForegroundColor Cyan
