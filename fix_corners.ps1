$files = Get-ChildItem -Path "Views\*.xaml"
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $updated = $content `
        -replace 'CornerRadius="0,6,6,6"', 'CornerRadius="12"' `
        -replace 'CornerRadius="6,6,0,0"', 'CornerRadius="12"' `
        -replace 'CornerRadius="6"', 'CornerRadius="12"' `
        -replace 'CornerRadius="8"', 'CornerRadius="12"' `
        -replace 'CornerRadius="5"', 'CornerRadius="8"'
    if ($updated -ne $content) {
        Set-Content $file.FullName $updated -NoNewline
        Write-Host "Updated: $($file.Name)"
    }
}
Write-Host "Done."
