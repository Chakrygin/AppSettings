@call powershell ^
    -NoLogo ^
    -NoProfile ^
    -NonInteractive ^
    -ExecutionPolicy "RemoteSigned" ^
    -File "%~dp0build.ps1" %*
