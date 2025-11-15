# Remove background from COFFEEBOTIC logo - Version 2 (More aggressive)

Add-Type -AssemblyName System.Drawing

# Load the source image
$sourceImage = [System.Drawing.Bitmap]::FromFile("$PSScriptRoot\Assets\Branding\coffeebotic.jpg")

# Create a new bitmap with alpha channel for transparency
$width = $sourceImage.Width
$height = $sourceImage.Height
$transparentBitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

# Sample the background color from top-left corner
$bgSample = $sourceImage.GetPixel(10, 10)
Write-Host "Background color detected: R=$($bgSample.R) G=$($bgSample.G) B=$($bgSample.B)"

# Use a larger tolerance for better background removal
$tolerance = 40

# Process each pixel
for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $pixel = $sourceImage.GetPixel($x, $y)

        # Calculate color distance from background
        $rDiff = [Math]::Abs($pixel.R - $bgSample.R)
        $gDiff = [Math]::Abs($pixel.G - $bgSample.G)
        $bDiff = [Math]::Abs($pixel.B - $bgSample.B)

        $isBackground = ($rDiff -le $tolerance) -and ($gDiff -le $tolerance) -and ($bDiff -le $tolerance)

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
