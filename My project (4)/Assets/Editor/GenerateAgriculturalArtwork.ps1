Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$artRoot = Join-Path $projectRoot "Assets/Art/Buildings/Agricultural_Module"

function New-Canvas([int]$width, [int]$height) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

function Get-Color([string]$hex) { return [System.Drawing.ColorTranslator]::FromHtml($hex) }
function New-Brush([string]$hex) { return [System.Drawing.SolidBrush]::new((Get-Color $hex)) }
function New-Pen([string]$hex, [float]$width) {
    $pen = [System.Drawing.Pen]::new((Get-Color $hex), $width)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    return $pen
}
function Fill-Round([System.Drawing.Graphics]$g, [float]$x, [float]$y, [float]$w, [float]$h, [float]$r, [System.Drawing.Brush]$brush) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.FillPath($brush, $path)
    $path.Dispose()
}
function Draw-Round([System.Drawing.Graphics]$g, [float]$x, [float]$y, [float]$w, [float]$h, [float]$r, [System.Drawing.Pen]$pen) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.DrawPath($pen, $path)
    $path.Dispose()
}
function Save-Canvas($canvas, [string]$path) {
    $directory = Split-Path -Parent $path
    if (!(Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Dispose()
}
function Draw-Bolt([System.Drawing.Graphics]$g, [float]$x, [float]$y, [float]$radius = 8) {
    $g.FillEllipse((New-Brush "#636E70"), $x-$radius, $y-$radius, $radius*2, $radius*2)
    $g.FillEllipse((New-Brush "#D9DCD7"), $x-$radius+3, $y-$radius+3, ($radius-3)*2, ($radius-3)*2)
    $g.DrawLine((New-Pen "#7A8383" 2), $x-3, $y, $x+3, $y)
}

function Draw-Leaf([System.Drawing.Graphics]$g, [float]$cx, [float]$cy, [float]$angle, [float]$length, [float]$halfWidth, [string]$baseHex, [string]$tipHex, [string]$outlineHex, [string]$veinHex, [bool]$lobed = $false) {
    $state = $g.Save()
    $g.TranslateTransform($cx, $cy)
    $g.RotateTransform($angle)
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.StartFigure()
    if ($lobed) {
        $path.AddBezier(0, 0, -$halfWidth*0.65, -$length*0.18, -$halfWidth*1.18, -$length*0.45, -$halfWidth*0.76, -$length*0.56)
        $path.AddBezier(-$halfWidth*0.76, -$length*0.56, -$halfWidth*1.30, -$length*0.72, -$halfWidth*0.46, -$length*0.80, -$halfWidth*0.52, -$length*0.91)
        $path.AddBezier(-$halfWidth*0.52, -$length*0.91, -$halfWidth*0.22, -$length*0.96, -$halfWidth*0.12, -$length, 0, -$length)
        $path.AddBezier(0, -$length, $halfWidth*0.22, -$length*0.96, $halfWidth*0.52, -$length*0.91, $halfWidth*0.52, -$length*0.91)
        $path.AddBezier($halfWidth*0.52, -$length*0.91, $halfWidth*0.46, -$length*0.80, $halfWidth*1.30, -$length*0.72, $halfWidth*0.76, -$length*0.56)
        $path.AddBezier($halfWidth*0.76, -$length*0.56, $halfWidth*1.18, -$length*0.45, $halfWidth*0.65, -$length*0.18, 0, 0)
    } else {
        $path.AddBezier(0, 0, -$halfWidth*0.72, -$length*0.20, -$halfWidth*1.08, -$length*0.72, 0, -$length)
        $path.AddBezier(0, -$length, $halfWidth*1.08, -$length*0.72, $halfWidth*0.72, -$length*0.20, 0, 0)
    }
    $path.CloseFigure()
    $bounds = [System.Drawing.RectangleF]::new(-$halfWidth*1.4, -$length, $halfWidth*2.8, $length)
    $fill = [System.Drawing.Drawing2D.LinearGradientBrush]::new($bounds, (Get-Color $baseHex), (Get-Color $tipHex), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
    $g.FillPath($fill, $path)
    $outline = New-Pen $outlineHex 4
    $g.DrawPath($outline, $path)
    $vein = New-Pen $veinHex 3
    $g.DrawBezier($vein, 0, -8, -$halfWidth*0.12, -$length*0.28, $halfWidth*0.16, -$length*0.67, 0, -$length+10)
    foreach ($fraction in @(0.34,0.52,0.69,0.82)) {
        $yy = -$length * $fraction
        $span = $halfWidth * (1 - $fraction*0.62)
        $g.DrawLine($vein, 0, $yy, -$span, $yy-10)
        $g.DrawLine($vein, 0, $yy, $span, $yy-10)
    }
    $vein.Dispose(); $outline.Dispose(); $fill.Dispose(); $path.Dispose()
    $g.Restore($state)
}

function New-Rosette([string]$name, [string]$family, [string]$stage) {
    $c = New-Canvas 640 640; $g = $c.Graphics
    $green = $family -eq "green"
    $red = $family -eq "red"
    if ($red) { $palette = @(@("#51334D","#8C4E70","#B96E88","#D58B99"),@("#63405C","#A45872","#D48A98","#F0B1A5")) }
    elseif ($family -eq "mizuna") { $palette = @(@("#294A36","#3E6E43","#75A550","#BDD27A"),@("#375C3C","#5C8645","#A9C45F","#E3E79A")) }
    else { $palette = @(@("#31513B","#548448","#84AE5D","#C2D887"),@("#41663C","#6F9848","#A9C765","#D9E69B")) }
    if ($stage -eq "seed") {
        $g.FillEllipse((New-Brush "#8E795C"), 252, 301, 136, 64)
        $g.FillEllipse((New-Brush "#B7A27D"), 264, 306, 112, 47)
        for ($i=0; $i -lt 5; $i++) {
            $x = 278 + (($i*29)%76); $y = 316 + (($i*17)%20)
            $seedColor = if ($family -eq "dwarf_wheat") { "#D9BB6A" } elseif ($red) { "#79575E" } else { "#72794C" }
            $g.FillEllipse((New-Brush $seedColor), $x, $y, 13, 8)
            $g.DrawArc((New-Pen "#D6C69E" 2), $x, $y, 13, 8, 185, 175)
        }
        Save-Canvas $c (Join-Path $artRoot "Plants/plant_cycle_${name}_seed.png")
        return
    }
    $scale = if ($stage -eq "growing") { 0.58 } else { 1.0 }
    $centerX = 320 + $(if($stage -eq "growing") { 8 } else { 0 })
    $centerY = 355
    $outerCount = if ($family -eq "mizuna") { $(if($stage -eq "growing"){11}else{18}) } else { $(if($stage -eq "growing"){10}else{15}) }
    for ($i=0; $i -lt $outerCount; $i++) {
        $angle = $i * (360.0 / $outerCount) + (($i*19)%23) - 11
        $length = (190 + (($i*37)%54) - 27) * $scale
        $width = if ($family -eq "mizuna") { (29 + (($i*17)%13)) * $scale } elseif ($green) { (53 + (($i*13)%19)) * $scale } else { (42 + (($i*23)%21)) * $scale }
        $shiftX = (($i*31)%25)-12; $shiftY = (($i*19)%19)-9
        Draw-Leaf $g ($centerX+$shiftX) ($centerY+$shiftY) $angle $length $width $palette[0][0] $palette[0][2] $palette[0][1] $palette[0][3] ($green -and ($i%2 -eq 0))
    }
    $innerCount = if ($stage -eq "growing") { 5 } else { 10 }
    for ($i=0; $i -lt $innerCount; $i++) {
        $angle = $i*(360.0/$innerCount) + 17 + (($i*11)%13)
        $length = (150 + (($i*29)%36)-18) * $scale
        $width = if ($family -eq "mizuna") { 24*$scale } elseif ($green) { 43*$scale } else { 36*$scale }
        Draw-Leaf $g ($centerX+(($i*17)%15)-7) ($centerY+(($i*13)%13)-6) $angle $length $width $palette[1][0] $palette[1][2] $palette[1][1] $palette[1][3] ($green -and ($i%2 -eq 1))
    }
    $coreSize = if ($stage -eq "growing") { 38 } else { 48 }
    $coreX=$centerX-$coreSize/2; $coreY=$centerY-$coreSize/2
    $g.FillEllipse((New-Brush $(if($red){"#B8C879"}else{"#AFC66D"})), $coreX, $coreY, $coreSize, $coreSize)
    $g.DrawEllipse((New-Pen "#3D593D" 5), $coreX, $coreY, $coreSize, $coreSize)
    Save-Canvas $c (Join-Path $artRoot "Plants/plant_cycle_${name}_${stage}.png")
}

function New-WheatCanopy([string]$stage) {
    $c = New-Canvas 640 640; $g = $c.Graphics
    if ($stage -eq "seed") {
        $g.FillEllipse((New-Brush "#8E795C"), 252, 301, 136, 64)
        $g.FillEllipse((New-Brush "#B7A27D"), 264, 306, 112, 47)
        for ($i=0; $i -lt 5; $i++) { $x=278+(($i*29)%76); $y=316+(($i*17)%20); $g.FillEllipse((New-Brush "#D9BB6A"),$x,$y,16,9) }
        Save-Canvas $c (Join-Path $artRoot "Plants/plant_cycle_dwarf_wheat_seed.png")
        return
    }
    $scale = if ($stage -eq "growing") { 0.62 } else { 1.0 }
    $stemCount = if ($stage -eq "growing") { 7 } else { 14 }
    for ($i=0; $i -lt $stemCount; $i++) {
        $baseX = 320 + (($i*43)%112)-56
        $tipX = $baseX + (($i*29)%74)-37
        $topY = 118 + (($i*31)%82)
        if ($stage -eq "growing") { $topY=270+(($i*17)%44) }
        $stem = New-Pen "#63834B" 5
        $g.DrawBezier($stem, [float]$baseX, 512, [float]($baseX-18), 390, [float]($tipX+16), 260, [float]$tipX, [float]$topY)
        for ($leaf=0; $leaf -lt 3; $leaf++) {
            $yy = 420-$leaf*70-(($i*17)%22)
            if ($stage -eq "growing") { $yy=440-$leaf*42-(($i*13)%15) }
            $dir = if (($i+$leaf)%2 -eq 0) { -1 } else { 1 }
            $length = (78 + (($i+$leaf*7)%30)) * $scale
            $blade = New-Pen "#6E9A52" (10*$scale)
            $shine = New-Pen "#B6CA79" (3*$scale)
            $sx = [float]$baseX; $ex = $sx+$dir*$length
            $g.DrawBezier($blade, $sx, [float]$yy, $sx+$dir*18, $yy-12, $sx+$dir*($length*0.70), $yy-35, $ex, $yy-47*$scale)
            $g.DrawBezier($shine, $sx+2, [float]($yy-1), $sx+$dir*20, $yy-14, $sx+$dir*($length*0.65), $yy-35, $ex-4*$dir, $yy-46*$scale)
        }
        if ($stage -eq "mature") {
            $grain = New-Brush "#C7A659"
            for ($k=0; $k -lt 7; $k++) {
                $gx = $tipX + $(if($k%2 -eq 0){-1}else{1})*(9+(($i+$k)%4)*3)
                $gy = $topY+20+$k*13
                $g.FillEllipse($grain, $gx-6, $gy-10, 12, 19)
                $g.DrawLine((New-Pen "#DCC987" 2), $tipX, $gy, $gx, $gy-9)
            }
        }
    }
    Save-Canvas $c (Join-Path $artRoot "Plants/plant_cycle_dwarf_wheat_${stage}.png")
}

function New-RedRobinTomato([string]$stage) {
    $c = New-Canvas 640 640; $g = $c.Graphics
    if ($stage -eq "seed") {
        $g.FillEllipse((New-Brush "#8E795C"), 218, 278, 204, 104)
        $g.FillEllipse((New-Brush "#B7A27D"), 232, 287, 176, 84)
        for ($i=0; $i -lt 6; $i++) {
            $x=260+(($i*31)%108); $y=308+(($i*17)%34)
            $g.FillEllipse((New-Brush "#9E7A50"),$x,$y,20,12)
            $g.DrawArc((New-Pen "#D6C69E" 2),$x,$y,20,12,185,175)
        }
        Save-Canvas $c (Join-Path $artRoot "Plants/plant_cycle_red_robin_tomato_seed.png")
        return
    }
    $scale = if ($stage -eq "growing") { 0.68 } else { 1.0 }
    $branchPen = New-Pen "#46683F" (8*$scale)
    $highlight = New-Pen "#9EB96B" (3*$scale)
    $centers = @(@(318,500),@(245,438),@(390,420),@(275,350),@(370,320),@(230,285),@(415,270))
    for ($i=0;$i -lt $centers.Count;$i++) {
        $bx=$centers[$i][0];$by=$centers[$i][1]
        if($stage -eq "growing") { $bx=320+($bx-320)*0.58; $by=420+($by-420)*0.62 }
        $g.DrawBezier($branchPen,320,535,[float](320+($bx-320)*0.24),[float](535-($by-535)*0.3),[float]($bx-8),[float]($by+45*$scale),[float]$bx,[float]$by)
        $g.DrawBezier($highlight,323,525,[float](320+($bx-320)*0.28),[float](525-($by-525)*0.3),[float]($bx-5),[float]($by+36*$scale),[float]$bx,[float]$by)
        if($i -gt 0) {
            Draw-Leaf $g ([float]($bx-32)) ([float]($by+18)) (220+(($i*41)%105)) (102*$scale) (29*$scale) "#42683E" "#88A952" "#355638" "#C5D68A" $false
            Draw-Leaf $g ([float]($bx+30)) ([float]($by+17)) (35+(($i*37)%110)) (94*$scale) (28*$scale) "#42683E" "#88A952" "#355638" "#C5D68A" $false
        }
    }
    if ($stage -eq "mature") {
        foreach ($fruit in @(@(220,375),@(415,360),@(280,250),@(445,230),@(340,195),@(180,300))) {
            $x=$fruit[0];$y=$fruit[1];$d=42+(($x+$y)%12)
            $g.FillEllipse((New-Brush "#A52F2C"),$x-$d/2+3,$y-$d/2+6,$d,$d)
            $g.FillEllipse((New-Brush "#E55645"),$x-$d/2,$y-$d/2,$d,$d)
            $g.FillEllipse((New-Brush "#F28B67"),$x-$d/2+7,$y-$d/2+6,$d*0.32,$d*0.25)
            for($leaf=0;$leaf -lt 5;$leaf++) {
                $a=$leaf*72*[Math]::PI/180
                $x1=$x+[Math]::Cos($a)*5;$y1=$y+[Math]::Sin($a)*5
                $x2=$x+[Math]::Cos($a)*16;$y2=$y+[Math]::Sin($a)*16
                $g.DrawLine((New-Pen "#4C783E" 4),[float]$x1,[float]$y1,[float]$x2,[float]$y2)
            }
        }
    }
    Save-Canvas $c (Join-Path $artRoot "Plants/plant_cycle_red_robin_tomato_${stage}.png")
}

function New-CropLabel([string]$filename, [string]$title, [string]$cultivar, [string]$accent) {
    $c=New-Canvas 640 240;$g=$c.Graphics
    # A low-profile angled terminal with a lit display, thick bezel and mounting feet.
    $g.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(45,20,24,25)),44,197,552,25)
    Fill-Round $g 30 180 84 34 8 (New-Brush "#626E70")
    Fill-Round $g 526 180 84 34 8 (New-Brush "#626E70")
    Fill-Round $g 18 12 604 200 26 (New-Brush "#354043")
    Fill-Round $g 27 20 586 180 21 (New-Brush "#929B98")
    Fill-Round $g 35 27 570 166 16 (New-Brush "#D5D7D0")
    Fill-Round $g 48 39 544 142 11 (New-Brush "#112328")
    Fill-Round $g 58 48 524 20 6 (New-Brush "#20393D")
    $g.FillEllipse((New-Brush $accent),70,54,9,9)
    $microFont=[System.Drawing.Font]::new("Consolas",12,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel)
    $titleFont=[System.Drawing.Font]::new("Segoe UI",28,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel)
    $subFont=[System.Drawing.Font]::new("Segoe UI",18,[System.Drawing.FontStyle]::Regular,[System.Drawing.GraphicsUnit]::Pixel)
    $metricFont=[System.Drawing.Font]::new("Consolas",11,[System.Drawing.FontStyle]::Regular,[System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString("CROP SYSTEM  /  GROUP IDENTIFICATION",$microFont,(New-Brush "#8DB7B2"),86,51)
    $g.DrawString($title,$titleFont,(New-Brush "#F0F2E9"),68,77)
    $g.DrawString($cultivar,$subFont,(New-Brush "#B6C6BF"),70,116)
    $g.DrawLine((New-Pen "#476064" 2),70,146,496,146)
    $g.DrawString("ROOT-ZONE   HYDROPONIC ARRAY",$metricFont,(New-Brush "#91AAA6"),70,153)
    Fill-Round $g 507 75 49 78 9 (New-Brush "#20383C")
    $g.FillEllipse((New-Brush $accent),517,87,29,29)
    $g.FillEllipse((New-Brush "#DCE8D9"),521,91,11,11)
    $g.DrawLine((New-Pen "#77B8B1" 3),520,126,543,126)
    $g.DrawLine((New-Pen "#77B8B1" 3),520,135,538,135)
    Fill-Round $g 566 84 12 67 5 (New-Brush "#273B3E")
    Fill-Round $g 569 126 6 22 3 (New-Brush $accent)
    foreach($point in @(@(38,32),@(602,32),@(38,188),@(602,188))){Draw-Bolt $g $point[0] $point[1] 5}
    Save-Canvas $c (Join-Path $artRoot "Cultivation/$filename.png")
}

function New-RowBench {
    $c = New-Canvas 2000 650; $g = $c.Graphics
    $shadow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(38,22,25,24))
    $g.FillEllipse($shadow, 70, 585, 1860, 40)
    Fill-Round $g 24 28 1952 592 42 (New-Brush "#4E585B")
    Fill-Round $g 38 40 1924 568 34 (New-Brush "#AEB4B1")
    Fill-Round $g 52 52 1896 544 28 (New-Brush "#E9E7DF")
    Draw-Round $g 52 52 1896 544 28 (New-Pen "#F8F7F0" 6)
    # Inset service rails, four corner brackets and a readable equipment label.
    Fill-Round $g 78 74 1844 38 16 (New-Brush "#D1D2CB")
    Fill-Round $g 86 78 1828 11 5 (New-Brush "#E7E6DD")
    Fill-Round $g 80 548 1840 22 10 (New-Brush "#5C676A")
    Fill-Round $g 86 553 1828 7 3 (New-Brush "#2D91A1")
    $font = [System.Drawing.Font]::new("Segoe UI", 19, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString("ROOT-ZONE GROW ARRAY  |  3 SINGLE-CROP ROWS", $font, (New-Brush "#536063"), 102, 79)
    $positions = @(250,500,750,1000,1250,1500,1750)
    $rowCenters = @(176,325,474)
    for ($row=0; $row -lt 3; $row++) {
        $cy = $rowCenters[$row]
        Fill-Round $g 90 ($cy-56) 1820 112 24 (New-Brush "#5E686A")
        Fill-Round $g 101 ($cy-47) 1798 94 19 (New-Brush "#202C2F")
        Fill-Round $g 110 ($cy-40) 1780 12 6 (New-Brush "#758182")
        Fill-Round $g 110 ($cy+28) 1780 7 3 (New-Brush "#10191B")
        # Water/nutrient service rails with distinct supply and return color codes.
        $g.DrawLine((New-Pen "#3F9BA7" 8), 122, $cy+43, 1878, $cy+43)
        $g.DrawLine((New-Pen "#BD8750" 6), 122, $cy+51, 1878, $cy+51)
        foreach ($px in $positions) {
            # Ceramic substrate pillow / root module.
            $g.FillEllipse((New-Brush "#111A1C"), $px-72, $cy-42, 144, 82)
            $g.FillEllipse((New-Brush "#59615C"), $px-65, $cy-38, 130, 73)
            $g.FillEllipse((New-Brush "#9E8F76"), $px-58, $cy-32, 116, 60)
            $g.FillEllipse((New-Brush "#C5B697"), $px-51, $cy-27, 102, 49)
            $g.DrawArc((New-Pen "#E0D5BF" 4), $px-48, $cy-24, 96, 43, 195, 148)
            $g.FillEllipse((New-Brush "#5E6963"), $px-13, $cy-8, 26, 18)
            $g.FillEllipse((New-Brush "#303A38"), $px-8, $cy-5, 16, 11)
            # Pillow quick-connect, visible at the lower service edge.
            $g.DrawLine((New-Pen "#6BC0C7" 5), $px+33, $cy+29, $px+33, $cy+44)
            $g.FillEllipse((New-Brush "#D8DAD2"), $px+25, $cy+37, 17, 14)
            $g.FillEllipse((New-Brush "#4CA0A8"), $px+29, $cy+41, 9, 7)
            Draw-Bolt $g ($px-81) $cy 5
            Draw-Bolt $g ($px+81) $cy 5
        }
        # LED growth strip on the north edge of each crop row.
        Fill-Round $g 115 ($cy-53) 1770 7 3 (New-Brush "#353B3B")
        for ($light=0; $light -lt 30; $light++) {
            $x = 137 + $light*60
            $tone = switch ($light%4) { 0 {"#C85E68"} 1 {"#5A83C8"} 2 {"#77B48D"} default {"#EEE5CF"} }
            $g.FillEllipse((New-Brush $tone), $x, $cy-52, 20, 5)
        }
        $labelFont = [System.Drawing.Font]::new("Segoe UI", 14, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $g.DrawString(("ROW {0:00}" -f ($row+1)), $labelFont, (New-Brush "#E5E2D7"), 100, $cy-8)
    }
    for ($x=90; $x -le 1910; $x+=364) { Draw-Bolt $g $x 59 7; Draw-Bolt $g $x 587 7 }
    Save-Canvas $c (Join-Path $artRoot "Cultivation/hydroponic_bench_3row.png")
}

function New-BulkheadSegment {
    $c = New-Canvas 180 860; $g = $c.Graphics
    Fill-Round $g 14 8 152 844 30 (New-Brush "#4F585B")
    Fill-Round $g 22 16 136 828 26 (New-Brush "#9EA5A2")
    Fill-Round $g 30 24 120 812 20 (New-Brush "#E4E2D9")
    Fill-Round $g 38 32 104 796 16 (New-Brush "#C6C5BD")
    Fill-Round $g 47 42 86 776 12 (New-Brush "#E9E7DF")
    $g.DrawLine((New-Pen "#737C7D" 5), 35, 70, 35, 790)
    $g.DrawLine((New-Pen "#F5F3EA" 4), 145, 70, 145, 790)
    foreach ($y in @(78,190,302,414,526,638,750,802)) { Draw-Bolt $g 90 $y 8 }
    for ($y=120; $y -lt 780; $y+=80) {
        Fill-Round $g 67 $y 46 22 8 (New-Brush "#596468")
        Fill-Round $g 72 ($y+5) 36 12 5 (New-Brush "#344348")
    }
    Save-Canvas $c (Join-Path $artRoot "Interior/room_bulkhead_segment.png")
}

function New-HatchAssets {
    $c = New-Canvas 220 720; $g = $c.Graphics
    Fill-Round $g 14 10 192 700 26 (New-Brush "#4F595C")
    Fill-Round $g 23 18 174 684 22 (New-Brush "#B8BFBB")
    Fill-Round $g 31 26 158 668 18 (New-Brush "#687275")
    Fill-Round $g 42 37 136 646 14 (New-Brush "#263337")
    Fill-Round $g 51 48 118 624 11 (New-Brush "#ECEAE1")
    Fill-Round $g 61 70 98 580 10 (New-Brush "#C9CBC4")
    Fill-Round $g 67 82 86 556 8 (New-Brush "#DADBD4")
    $g.DrawLine((New-Pen "#899190" 5), 76, 107, 76, 620)
    $g.DrawLine((New-Pen "#F5F4EE" 4), 145, 107, 145, 620)
    foreach ($y in @(115,250,385,520,615)) {
        Fill-Round $g 82 ($y-8) 56 16 7 (New-Brush "#687376")
        Fill-Round $g 88 ($y-4) 44 8 4 (New-Brush "#D7D9D2")
    }
    foreach ($y in @(60,660)) { Draw-Bolt $g 47 $y 7; Draw-Bolt $g 173 $y 7 }
    Save-Canvas $c (Join-Path $artRoot "Airlock/airlock_hatch_wall_frame_side.png")

    $c = New-Canvas 180 620; $g = $c.Graphics
    Fill-Round $g 15 12 150 596 22 (New-Brush "#434D50")
    Fill-Round $g 24 22 132 576 18 (New-Brush "#ABB2AF")
    Fill-Round $g 33 31 114 558 13 (New-Brush "#737D7F")
    Fill-Round $g 42 43 96 534 10 (New-Brush "#D8D9D2")
    Fill-Round $g 51 58 78 504 9 (New-Brush "#C3C7C1")
    $g.DrawLine((New-Pen "#EFF0EA" 4), 62, 79, 62, 540)
    for ($y=92; $y -lt 520; $y+=72) { $g.DrawLine((New-Pen "#929A98" 3), 69, $y, 111, $y) }
    Fill-Round $g 68 88 44 26 8 (New-Brush "#35464A")
    foreach ($y in @(68,592)) { Draw-Bolt $g 90 $y 6 }
    Save-Canvas $c (Join-Path $artRoot "Airlock/airlock_pressure_gate_leaf_side.png")
}

function New-DeconConsole {
    $c = New-Canvas 220 720; $g = $c.Graphics
    Fill-Round $g 38 12 144 696 22 (New-Brush "#505A5D")
    Fill-Round $g 46 20 128 680 18 (New-Brush "#AEB4B0")
    Fill-Round $g 54 29 112 660 15 (New-Brush "#E7E5DC")
    Fill-Round $g 63 40 94 174 12 (New-Brush "#263438")
    Fill-Round $g 70 48 80 152 8 (New-Brush "#34494D")
    $g.DrawLine((New-Pen "#5DC3C3" 7), 79, 169, 139, 169)
    $g.DrawLine((New-Pen "#5DC3C3" 5), 79, 151, 126, 151)
    $g.DrawLine((New-Pen "#5DC3C3" 4), 79, 134, 115, 134)
    $lampX = 82
    foreach ($pair in @(@("#C75249",72),@("#E2B14F",98),@("#54A88E",124))) {
        $g.FillEllipse((New-Brush "#465153"), $lampX-11, $pair[1]-11, 22, 22)
        $g.FillEllipse((New-Brush $pair[0]), $lampX-6, $pair[1]-6, 12, 12)
        Fill-Round $g 106 ($pair[1]-4) 46 8 4 (New-Brush "#7C8683")
    }
    Fill-Round $g 68 253 84 116 13 (New-Brush "#D2D0C7")
    Draw-Round $g 68 253 84 116 13 (New-Pen "#838C8B" 4)
    Fill-Round $g 77 263 66 96 9 (New-Brush "#B8B7AE")
    Fill-Round $g 87 278 46 65 14 (New-Brush "#586266")
    Fill-Round $g 94 285 32 49 10 (New-Brush "#38464A")
    Fill-Round $g 77 395 66 66 12 (New-Brush "#C3C1B8")
    Draw-Round $g 77 395 66 66 12 (New-Pen "#747E7E" 4)
    $g.FillEllipse((New-Brush "#304247"), 94, 412, 32, 32)
    $g.FillEllipse((New-Brush "#50B5AC"), 103, 421, 14, 14)
    Fill-Round $g 72 497 76 126 10 (New-Brush "#D1D0C8")
    Draw-Round $g 72 497 76 126 10 (New-Pen "#838B88" 4)
    for ($i=0; $i -lt 5; $i++) { Fill-Round $g 84 (510+$i*20) 52 8 4 (New-Brush $(if($i -eq 0){"#5DAA90"}else{"#828B88"})) }
    foreach ($y in @(47,672)) { Draw-Bolt $g 55 $y 6; Draw-Bolt $g 165 $y 6 }
    Save-Canvas $c (Join-Path $artRoot "Airlock/decontamination_wall_console.png")

    $c = New-Canvas 220 560; $g = $c.Graphics
    Fill-Round $g 48 12 124 536 20 (New-Brush "#4F595C")
    Fill-Round $g 57 20 106 520 16 (New-Brush "#ADB3AF")
    Fill-Round $g 65 29 90 502 12 (New-Brush "#E7E5DC")
    Fill-Round $g 77 54 66 94 10 (New-Brush "#2B3C40")
    Fill-Round $g 84 63 52 74 7 (New-Brush "#34494C")
    $g.DrawLine((New-Pen "#75C7C7" 5), 91, 125, 129, 125)
    foreach ($spec in @(@("#C75249",190),@("#D7A944",229),@("#53A78D",268))) {
        $g.FillEllipse((New-Brush "#475254"), 83, $spec[1]-12, 24, 24)
        $g.FillEllipse((New-Brush $spec[0]), 89, $spec[1]-6, 12, 12)
    }
    Fill-Round $g 91 310 38 91 11 (New-Brush "#BCC0B9")
    Draw-Round $g 91 310 38 91 11 (New-Pen "#758082" 4)
    Fill-Round $g 98 325 24 60 8 (New-Brush "#39484B")
    $g.FillEllipse((New-Brush "#59B6AA"), 104, 341, 12, 12)
    Fill-Round $g 82 430 54 77 9 (New-Brush "#C9C9C1")
    for ($i=0;$i -lt 4;$i++) { Fill-Round $g 91 (440+$i*15) 36 6 3 (New-Brush "#7D8784") }
    foreach ($y in @(47,513)) { Draw-Bolt $g 64 $y 5; Draw-Bolt $g 158 $y 5 }
    Save-Canvas $c (Join-Path $artRoot "Airlock/pressure_gate_wall_console.png")

    $c = New-Canvas 200 200; $g = $c.Graphics
    $g.FillEllipse((New-Brush "#20292B"), 12,12,176,176)
    $g.FillEllipse((New-Brush "#6B7474"), 20,20,160,160)
    $g.FillEllipse((New-Brush "#AEB5B0"), 28,28,144,144)
    $g.FillEllipse((New-Brush "#263236"), 39,39,122,122)
    $g.FillEllipse((New-Brush "#3C4A4C"), 50,50,100,100)
    for ($a=0; $a -lt 360; $a+=30) {
        $rad=$a*[Math]::PI/180; $x1=100+[Math]::Cos($rad)*59; $y1=100+[Math]::Sin($rad)*59; $x2=100+[Math]::Cos($rad)*76; $y2=100+[Math]::Sin($rad)*76
        $g.DrawLine((New-Pen "#D5D7CF" 5), [float]$x1,[float]$y1,[float]$x2,[float]$y2)
    }
    $g.FillEllipse((New-Brush "#59B6AA"), 81,81,38,38)
    Save-Canvas $c (Join-Path $artRoot "Airlock/decon_air_jet_nozzle.png")
}

New-RowBench
New-BulkheadSegment
New-HatchAssets
New-DeconConsole
foreach ($stage in @("seed","growing","mature")) {
    New-Rosette "red_romaine" "red" $stage
    New-Rosette "green_lettuce" "green" $stage
    New-Rosette "mizuna" "mizuna" $stage
    New-WheatCanopy $stage
    New-RedRobinTomato $stage
}
New-CropLabel "crop_label_red_romaine" "LECHUGA ROMANA ROJA" "Outredgeous · hojas comestibles" "#995775"
New-CropLabel "crop_label_mizuna" "MIZUNA" "Mostaza japonesa · hojas comestibles" "#6C9852"
New-CropLabel "crop_label_dwarf_wheat" "TRIGO ENANO" "Apogee · grano experimental" "#C5A55D"
New-CropLabel "crop_label_red_robin_tomato" "TOMATE CHERRY ENANO" "Red Robin · fruto comestible" "#B74C42"
Write-Host "Generated redesigned crop rows, crop canopy sprites, side-view airlock assets, decon console and bulkhead artwork."
