cd /home/max/job/rma/server-manager

# 1) Corrigir ownership dos diretórios usados no build
docker run --rm -v "$PWD":/src alpine:3.20 sh -lc \
  "chown -R $(id -u):$(id -g) /src/src/SessionManager.Agent.Windows /src/out"

# 2) Limpar artefatos antigos
rm -rf src/SessionManager.Agent.Windows/bin src/SessionManager.Agent.Windows/obj out/agent-win-x64
mkdir -p out

# 3) Publish win-x64
docker run --rm \
  --user "$(id -u):$(id -g)" \
  -v "$PWD":/src -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet publish src/SessionManager.Agent.Windows/SessionManager.Agent.Windows.csproj \
    -c Release -r win-x64 --self-contained true \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    -o /src/out/agent-win-x64
