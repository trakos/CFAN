
@if exist "%ProgramFiles%\Mono\bin" set PATH=%ProgramFiles%\Mono\bin;%PATH%
@if exist "%ProgramFiles(x86)%\Mono\bin" set PATH=%ProgramFiles(x86)%\Mono\bin;%PATH%

@if exist "%ProgramFiles%\Git\usr\bin" set PATH=%PATH%;%ProgramFiles%\Git\usr\bin
@if exist "%ProgramFiles(x86)%\Git\usr\bin" set PATH=%PATH%;%ProgramFiles(x86)%\Git\usr\bin

perl bin/build