if (Test-Path Env:ARIS_TEST_CONNECTION) {
    Remove-Item Env:ARIS_TEST_CONNECTION
    Write-Host '$env:ARIS_TEST_CONNECTION has been removed.' -ForegroundColor Yellow
}
else {
    Write-Host '$env:ARIS_TEST_CONNECTION is already unset.' -ForegroundColor DarkGray
}