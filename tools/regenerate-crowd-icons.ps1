Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"

$resourceDir = "C:\VS Code\GhCrowdFlow-release\src\GrasshopperComponents\Resources"

function New-IconCanvas([int]$size = 24) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    return @{
        Bitmap = $bmp
        Graphics = $g
    }
}

function Save-Icon($canvas, [string]$name) {
    $path = Join-Path $resourceDir $name
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Bitmap.Dispose()
}

function New-Pen([string]$hex, [float]$width = 2.0) {
    $color = [System.Drawing.ColorTranslator]::FromHtml($hex)
    $pen = New-Object System.Drawing.Pen($color, $width)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    return $pen
}

function New-Brush([string]$hex) {
    return New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml($hex))
}

function Fill-Rect($g, $brush, [float]$x, [float]$y, [float]$w, [float]$h) {
    $g.FillRectangle($brush, $x, $y, $w, $h)
}

function Draw-Rect($g, $pen, [float]$x, [float]$y, [float]$w, [float]$h) {
    $g.DrawRectangle($pen, $x, $y, $w, $h)
}

function Draw-ArrowHead($g, $brush, [float]$tipX, [float]$tipY, [float]$leftX, [float]$leftY, [float]$rightX, [float]$rightY) {
    $pts = New-Object 'System.Drawing.PointF[]' 3
    $pts[0] = New-Object System.Drawing.PointF($tipX, $tipY)
    $pts[1] = New-Object System.Drawing.PointF($leftX, $leftY)
    $pts[2] = New-Object System.Drawing.PointF($rightX, $rightY)
    $g.FillPolygon($brush, $pts)
}

$outline = "#263342"
$outlineSoft = "#3A4756"
$orange = "#F3AA1A"
$orangeLight = "#FFD86A"
$blue = "#39B8FF"
$blueLight = "#9DE1FF"
$green = "#7BE36B"
$greenLight = "#C6F8B7"
$cyan = "#5FE0E7"
$cyanLight = "#B9F8FC"
$yellow = "#FFE24B"
$yellowLight = "#FFF3A6"
$red = "#FF6B57"
$grayFill = "#E7ECF2"

# CrowdAgent
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.9
$fill = New-Brush $blue
$fillLight = New-Brush $blueLight
$g.FillEllipse($fill, 8.8, 2.8, 6.4, 6.4)
$g.DrawEllipse($outlinePen, 8.8, 2.8, 6.4, 6.4)
$g.FillEllipse($fillLight, 9.9, 3.8, 2.2, 2.2)
$g.DrawLine($outlinePen, 12, 9.2, 12, 15.1)
$g.DrawLine($outlinePen, 8.6, 12.1, 12, 10.5)
$g.DrawLine($outlinePen, 15.4, 12.1, 12, 10.5)
$g.DrawLine($outlinePen, 12, 15.1, 9.2, 20.1)
$g.DrawLine($outlinePen, 12, 15.1, 14.8, 20.1)
$outlinePen.Dispose(); $fill.Dispose(); $fillLight.Dispose()
Save-Icon $c "CrowdAgent.png"

# CrowdSource
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.9
$arrowPen = New-Pen $green 2.3
$arrowBrush = New-Brush $green
$doorFill = New-Brush $grayFill
Fill-Rect $g $doorFill 16 5 3.5 14
Draw-Rect $g $outlinePen 16 5 3.5 14
$g.DrawLine($arrowPen, 4, 12, 13.5, 12)
Draw-ArrowHead $g $arrowBrush 16.2 12 12.8 9.2 12.8 14.8
$outlinePen.Dispose(); $arrowPen.Dispose(); $arrowBrush.Dispose(); $doorFill.Dispose()
Save-Icon $c "CrowdSource.png"

# CrowdExit
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.9
$arrowPen = New-Pen $orange 2.3
$arrowBrush = New-Brush $orange
$doorFill = New-Brush $grayFill
Fill-Rect $g $doorFill 4.5 5 3.5 14
Draw-Rect $g $outlinePen 4.5 5 3.5 14
$g.DrawLine($arrowPen, 8.8, 12, 18.3, 12)
Draw-ArrowHead $g $arrowBrush 20.4 12 16.8 9.2 16.8 14.8
$outlinePen.Dispose(); $arrowPen.Dispose(); $arrowBrush.Dispose(); $doorFill.Dispose()
Save-Icon $c "CrowdExit.png"

# CrowdFloor
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.7
$gridPen = New-Pen $outlineSoft 1.2
$routePen = New-Pen $blue 2.1
$tile = New-Brush $cyanLight
$pts = New-Object 'System.Drawing.PointF[]' 4
$pts[0] = New-Object System.Drawing.PointF(4.5, 7.5)
$pts[1] = New-Object System.Drawing.PointF(18.5, 5.5)
$pts[2] = New-Object System.Drawing.PointF(19.5, 16.5)
$pts[3] = New-Object System.Drawing.PointF(5.5, 18.5)
$g.FillPolygon($tile, $pts)
$g.DrawPolygon($outlinePen, $pts)
$g.DrawLine($gridPen, 8.5, 6.8, 9, 17.8)
$g.DrawLine($gridPen, 13.2, 6.2, 14, 17.1)
$g.DrawLine($gridPen, 5.5, 11.5, 19.1, 9.8)
$g.DrawLine($gridPen, 5.9, 15.2, 19.3, 13.5)
$g.DrawLine($routePen, 6.2, 15.8, 10.1, 12.2)
$g.DrawLine($routePen, 10.1, 12.2, 13.1, 13.1)
$g.DrawLine($routePen, 13.1, 13.1, 17.2, 9.1)
$outlinePen.Dispose(); $gridPen.Dispose(); $routePen.Dispose(); $tile.Dispose()
Save-Icon $c "CrowdFloor.png"

# CrowdObstacle
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.9
$warnPen = New-Pen $red 2.4
$blockFill = New-Brush $orangeLight
Fill-Rect $g $blockFill 5.4 6 13.2 12
Draw-Rect $g $outlinePen 5.4 6 13.2 12
$g.DrawLine($warnPen, 8, 16, 16.2, 8)
$outlinePen.Dispose(); $warnPen.Dispose(); $blockFill.Dispose()
Save-Icon $c "CrowdObstacle.png"

# CrowdModel
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.7
$edgePen = New-Pen $orange 1.9
$nodeFill = New-Brush $yellow
$nodeFill2 = New-Brush $orangeLight
$g.DrawRectangle($outlinePen, 4.5, 5.5, 15, 13)
$g.DrawLine($edgePen, 8, 9, 12, 13)
$g.DrawLine($edgePen, 12, 13, 16, 9)
$g.DrawLine($edgePen, 8, 9, 16, 9)
$g.FillEllipse($nodeFill, 6.5, 7.3, 3.4, 3.4)
$g.FillEllipse($nodeFill2, 10.3, 11.2, 3.4, 3.4)
$g.FillEllipse($nodeFill, 14.1, 7.3, 3.4, 3.4)
$g.DrawEllipse($outlinePen, 6.5, 7.3, 3.4, 3.4)
$g.DrawEllipse($outlinePen, 10.3, 11.2, 3.4, 3.4)
$g.DrawEllipse($outlinePen, 14.1, 7.3, 3.4, 3.4)
$outlinePen.Dispose(); $edgePen.Dispose(); $nodeFill.Dispose(); $nodeFill2.Dispose()
Save-Icon $c "CrowdModel.png"

# CrowdHeatmap
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.5
$pathPen = New-Pen "#FFFFFF" 2.1
$cells = @(
    @{ X = 4.5; Y = 5.5; Color = $blueLight },
    @{ X = 12.5; Y = 5.5; Color = $yellowLight },
    @{ X = 4.5; Y = 13.5; Color = $cyan },
    @{ X = 12.5; Y = 13.5; Color = $orange }
)
foreach ($cell in $cells) {
    $brush = New-Brush $cell.Color
    Fill-Rect $g $brush $cell.X $cell.Y 7 7
    Draw-Rect $g $outlinePen $cell.X $cell.Y 7 7
    $brush.Dispose()
}
$g.DrawLine($pathPen, 5.6, 18.1, 9.9, 13.2)
$g.DrawLine($pathPen, 9.9, 13.2, 13.2, 13.2)
$g.DrawLine($pathPen, 13.2, 13.2, 18.4, 8.1)
$outlinePen.Dispose(); $pathPen.Dispose()
Save-Icon $c "CrowdHeatmap.png"

# CrowdHeatmapLegend
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 1.5
$legendColors = @($blue, $cyan, $yellow, $orange)
$y = 5
foreach ($color in $legendColors) {
    $brush = New-Brush $color
    Fill-Rect $g $brush 5  $y 5 3.5
    $brush.Dispose()
    $y += 3.5
}
Draw-Rect $g $outlinePen 5 5 5 14
$g.DrawLine($outlinePen, 13, 6, 18, 6)
$g.DrawLine($outlinePen, 13, 10.5, 18, 10.5)
$g.DrawLine($outlinePen, 13, 15, 18, 15)
$g.DrawLine($outlinePen, 13, 19, 18, 19)
$outlinePen.Dispose()
Save-Icon $c "CrowdHeatmapLegend.png"

# CrowdRun
$c = New-IconCanvas
$g = $c.Graphics
$trailPen = New-Pen $outline 2.4
$playBrush = New-Brush $cyan
$g.DrawArc($trailPen, 3.8, 5.8, 8.2, 8.2, 120, 180)
$pts = New-Object 'System.Drawing.PointF[]' 3
$pts[0] = New-Object System.Drawing.PointF(10, 6)
$pts[1] = New-Object System.Drawing.PointF(20, 12)
$pts[2] = New-Object System.Drawing.PointF(10, 18)
$g.FillPolygon($playBrush, $pts)
$trailPen.Dispose(); $playBrush.Dispose()
Save-Icon $c "CrowdRun.png"

# CrowdExportImage
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 2.0
$arrowPen = New-Pen $blue 2.5
$frameFill = New-Brush $grayFill
Fill-Rect $g $frameFill 4.5 4.5 13 11
Draw-Rect $g $outlinePen 4.5 4.5 13 11
$g.DrawLine($outlinePen, 6.6, 13.4, 10, 10)
$g.DrawLine($outlinePen, 10, 10, 12.4, 12)
$g.DrawLine($outlinePen, 12.4, 12, 15.5, 8.1)
$g.DrawEllipse($outlinePen, 13.2, 6.9, 2.2, 2.2)
$g.DrawLine($arrowPen, 12, 16.4, 12, 21.1)
$g.DrawLine($arrowPen, 9.6, 18.7, 12, 21.1)
$g.DrawLine($arrowPen, 14.4, 18.7, 12, 21.1)
$outlinePen.Dispose(); $arrowPen.Dispose(); $frameFill.Dispose()
Save-Icon $c "CrowdExportImage.png"

# CrowdExportReport
$c = New-IconCanvas
$g = $c.Graphics
$outlinePen = New-Pen $outline 2.0
$barPen = New-Pen $orange 2.8
$arrowPen = New-Pen $orange 2.5
$docFill = New-Brush $grayFill
$pts = New-Object 'System.Drawing.PointF[]' 5
$pts[0] = New-Object System.Drawing.PointF(6, 4.5)
$pts[1] = New-Object System.Drawing.PointF(14, 4.5)
$pts[2] = New-Object System.Drawing.PointF(18, 8.5)
$pts[3] = New-Object System.Drawing.PointF(18, 19)
$pts[4] = New-Object System.Drawing.PointF(6, 19)
$g.FillPolygon($docFill, $pts)
$g.DrawLine($outlinePen, 6, 4.5, 14, 4.5)
$g.DrawLine($outlinePen, 6, 4.5, 6, 19)
$g.DrawLine($outlinePen, 6, 19, 18, 19)
$g.DrawLine($outlinePen, 18, 8.5, 18, 19)
$g.DrawLine($outlinePen, 14, 4.5, 18, 8.5)
$g.DrawLine($barPen, 9, 14.5, 9, 10.7)
$g.DrawLine($barPen, 12, 14.5, 12, 8.8)
$g.DrawLine($barPen, 15, 14.5, 15, 11.7)
$g.DrawLine($arrowPen, 11, 21, 18.5, 21)
$g.DrawLine($arrowPen, 15.8, 18.5, 18.5, 21)
$g.DrawLine($arrowPen, 15.8, 23.5, 18.5, 21)
$outlinePen.Dispose(); $barPen.Dispose(); $arrowPen.Dispose(); $docFill.Dispose()
Save-Icon $c "CrowdExportReport.png"

# PluginIcon
$c = New-IconCanvas 64
$g = $c.Graphics
$outlinePen = New-Pen $outline 4.0
$routePen = New-Pen $orange 4.4
$nodeFill = New-Brush $yellow
$planeFill = New-Brush $cyanLight
$pts = New-Object 'System.Drawing.PointF[]' 4
$pts[0] = New-Object System.Drawing.PointF(10, 18)
$pts[1] = New-Object System.Drawing.PointF(54, 12)
$pts[2] = New-Object System.Drawing.PointF(54, 40)
$pts[3] = New-Object System.Drawing.PointF(10, 46)
$g.FillPolygon($planeFill, $pts)
$g.DrawLine($outlinePen, 10, 18, 22, 13)
$g.DrawLine($outlinePen, 22, 13, 39, 18)
$g.DrawLine($outlinePen, 39, 18, 54, 12)
$g.DrawLine($outlinePen, 10, 18, 10, 46)
$g.DrawLine($outlinePen, 22, 13, 22, 51)
$g.DrawLine($outlinePen, 39, 18, 39, 46)
$g.DrawLine($outlinePen, 54, 12, 54, 40)
$g.DrawLine($outlinePen, 10, 46, 22, 41)
$g.DrawLine($outlinePen, 22, 41, 39, 46)
$g.DrawLine($outlinePen, 39, 46, 54, 40)
$g.DrawLine($routePen, 16, 40, 27, 30)
$g.DrawLine($routePen, 27, 30, 36, 31)
$g.DrawLine($routePen, 36, 31, 47, 20)
$g.FillEllipse($nodeFill, 42, 15, 9, 9)
$g.DrawEllipse($outlinePen, 42, 15, 9, 9)
$outlinePen.Dispose(); $routePen.Dispose(); $nodeFill.Dispose(); $planeFill.Dispose()
Save-Icon $c "PluginIcon.png"

Write-Output 'ICONS_REGENERATED_RHINO_STYLE'
