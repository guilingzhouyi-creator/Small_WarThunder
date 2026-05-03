$dirs = @('GameSwitchSceneList', 'Desktop', 'PhysicMaterial', 'SOManager', 'TankSO')
foreach ($d in $dirs) {
    $dirPath = "Assets/$d"
    $metaPath = "$dirPath.meta"
    if (Test-Path $dirPath) {
        Remove-Item $dirPath -Recurse -Force
        Write-Host "Deleted directory: $dirPath"
    }
    if (Test-Path $metaPath) {
        Remove-Item $metaPath -Force
        Write-Host "Deleted meta: $metaPath"
    }
    else {
        Write-Host "Already clean: $d"
    }
}
