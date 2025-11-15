# Remove black backgrounds from organic coffee images

Add-Type -AssemblyName System.Drawing

function Remove-BlackBackground {
    param(
        [string]$InputPath,
        [string]$OutputPath
    )

    Write-Host "Processing: $InputPath"

    # Load the source image
    $sourceImage = [System.Drawing.Bitmap]::FromFile($InputPath)

    # Create a new bitmap with alpha channel for transparency
    $width = $sourceImage.Width
    $height = $sourceImage.Height
    $transparentBitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    # Black color threshold (remove pure black and near-black)
    $threshold = 30

    # Process each pixel
    for ($y = 0; $y -lt $height; $y++) {
        for ($x = 0; $x -lt $width; $x++) {
            $pixel = $sourceImage.GetPixel($x, $y)

            # Check if pixel is black or very dark
            $isBlack = ($pixel.R -le $threshold) -and ($pixel.G -le $threshold) -and ($pixel.B -le $threshold)

            if ($isBlack) {
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
            Write-Host "`r  Progress: $percent%" -NoNewline
        }
    }

    Write-Host "`r  Progress: 100%"

    # Save as PNG with transparency
    $transparentBitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

    # Cleanup
    $transparentBitmap.Dispose()
    $sourceImage.Dispose()

    Write-Host "  ✓ Saved: $OutputPath"
}

# Process organic1.jpg
Remove-BlackBackground -InputPath "$PSScriptRoot\Assets\Branding\organic1.jpg" -OutputPath "$PSScriptRoot\Assets\Branding\organic1-transparent.png"

# Process organic4.jpg
Remove-BlackBackground -InputPath "$PSScriptRoot\Assets\Branding\organic4.jpg" -OutputPath "$PSScriptRoot\Assets\Branding\organic4-transparent.png"

Write-Host ""
Write-Host "✓ All backgrounds removed successfully!" -ForegroundColor Green
Write-Host "  - organic1-transparent.png" -ForegroundColor Cyan
Write-Host "  - organic4-transparent.png" -ForegroundColor Cyan
