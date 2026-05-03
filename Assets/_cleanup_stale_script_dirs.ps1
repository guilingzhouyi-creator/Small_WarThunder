$dirs = @('TankerController', 'TankFireController', 'MoveManager', 'ConnonBall', 'UIController', 'AudioManager', 'CameraManager', 'WeatherController', 'GameLevelManager', 'GameManager', 'ObjectSystem')
foreach ($d in $dirs) {
    $p = "Assets/Scripts/$d"
    if (Test-Path $p) { 
        Remove-Item $p -Recurse -Force; Write-Host "Deleted dir: $p" 
    }
    $mp = "$p.meta"
    if (Test-Path $mp) { 
        Remove-Item $mp -Force; Write-Host "Deleted meta: $mp" 
    }
    Write-Host "DONE: $d"
}
