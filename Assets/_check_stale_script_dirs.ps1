$oldDirs = @('TankerController', 'TankFireController', 'MoveManager', 'ConnonBall', 'UIController', 'AudioManager', 'CameraManager', 'WeatherController', 'GameLevelManager', 'GameManager', 'ObjectSystem')
foreach ($d in $oldDirs) {
    $path = "Assets/Scripts/$d"
    $files = Get-ChildItem $path -Recurse -File -Exclude '*.meta' -ErrorAction SilentlyContinue
    if ($files) { 
        Write-Host ("--- STILL HAS FILES: $d ($($files.Count)) ---")
        $files | ForEach-Object { Write-Host $_.FullName }
    }
    else { 
        Write-Host "EMPTY: $d" 
    }
    Write-Host ''
}
