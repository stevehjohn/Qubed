rm *.Host.*.zip
dotnet publish ./src/Qubed.FrontEnd/Qubed.FrontEnd.csproj -c Release -r osx-x64 /p:PublishReadyToRun=true /p:TieredCompilation=false --self-contained /p:PublishReadyToRunShowWarnings=true
dotnet publish ./src/Qubed.FrontEnd/Qubed.FrontEnd.csproj -c Release -r osx-arm64 /p:PublishReadyToRun=true /p:TieredCompilation=false /p:PublishReadyToRunShowWarnings=true
dotnet publish ./src/Qubed.FrontEnd/Qubed.FrontEnd.csproj -c Release -r win-x64 /p:PublishReadyToRun=true /p:TieredCompilation=false --self-contained /p:PublishReadyToRunShowWarnings=true
dotnet publish ./src/Qubed.FrontEnd/Qubed.FrontEnd.csproj -c Release -r linux-x64 /p:PublishReadyToRun=true /p:TieredCompilation=false --self-contained /p:PublishReadyToRunShowWarnings=true
dotnet publish ./src/Qubed.FrontEnd/Qubed.FrontEnd.csproj -c Release -r linux-arm64 /p:PublishReadyToRun=true /p:TieredCompilation=false --self-contained /p:PublishReadyToRunShowWarnings=true

cd ./src/Qubed.FrontEnd/bin/Release/*/osx-x64/publish
pwd
chmod +xx Qubed.FrontEnd
rm *.zip
zip -r Qubed.FrontEnd.macOS.Intel.zip *
cd -
mv ./src/Qubed.FrontEnd/bin/Release/*/osx-x64/publish/Qubed.FrontEnd.macOS.Intel.zip .

cd ./src/Qubed.FrontEnd/bin/Release/*/osx-arm64/publish
pwd
chmod +xx Qubed.FrontEnd
rm *.zip
zip -r Qubed.FrontEnd.macOS.Apple.zip *
cd -
mv ./src/Qubed.FrontEnd/bin/Release/*/osx-arm64/publish/Qubed.FrontEnd.macOS.Apple.zip .

cd ./src/Qubed.FrontEnd/bin/Release/*/win-x64/publish
pwd
rm *.zip
zip -r Qubed.FrontEnd.Windows.Intel.zip *
cd -
mv ./src/Qubed.FrontEnd/bin/Release/*/win-x64/publish/Qubed.FrontEnd.Windows.Intel.zip .

cd ./src/Qubed.FrontEnd/bin/Release/*/linux-x64/publish
pwd
rm *.zip
zip -r Qubed.FrontEnd.Linux.Intel.zip *
cd -
mv ./src/Qubed.FrontEnd/bin/Release/*/linux-x64/publish/Qubed.FrontEnd.Linux.Intel.zip .

cd ./src/Qubed.FrontEnd/bin/Release/*/linux-arm64/publish
pwd
rm *.zip
zip -r Qubed.FrontEnd.Linux.Arm.zip *
cd -
mv ./src/Qubed.FrontEnd/bin/Release/*/linux-arm64/publish/Qubed.FrontEnd.Linux.Arm.zip .

ls *.zip
