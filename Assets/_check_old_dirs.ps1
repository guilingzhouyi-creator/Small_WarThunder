$dirs = @('GameMainAllSystem', 'GameSwitchSceneList', 'Desktop', 'PhysicMaterial', 'SOManager', 'TankSO', '_Recovery', 'GamePlayer', 'prefabs', 'Resources', 'Unity Asset Management System', 'Miscellaneous Management System')
foreach ($d in $dirs) {
    $path = "Assets/$d"
    $files = Get-ChildItem $path -Recurse -File -Exclude '*.meta' -ErrorAction SilentlyContinue
    if ($files) { 
        Write-Host ("--- $d ($($files.Count) files) ---")
        $files | ForEach-Object { Write-Host $_.FullName }
    }
    else { 
        Write-Host "--- $d (EMPTY) ---" 
    }
    Write-Host ''
}
