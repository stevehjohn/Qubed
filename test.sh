dotnet build src --configuration Release

dotnet test src \
  --no-build \
  --configuration Release \
  --logger "console;verbosity=detailed"
