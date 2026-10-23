#!/usr/bin/env bash
set -euo pipefail
# Run after run-linux.sh in the same /work directory and NuGet cache.
cd /work
mkdir -p /results/packages
for project in src/*/*.csproj; do
  dotnet pack "$project" -c Release -o /results/packages
done
# Keep SDK packages from earlier development builds out of the consumer.
# Reuse immutable third-party packages without downloading the toolchain again.
stamp=$(sha256sum /results/packages/*.nupkg | sha256sum | cut -d ' ' -f 1)
export NUGET_PACKAGES="/work/package-cache/$stamp"
if [ ! -d "$NUGET_PACKAGES" ]; then
  mkdir -p "$NUGET_PACKAGES"
  for package in /root/.nuget/packages/*; do
    case "$(basename "$package")" in rednb.nacos*) continue ;; esac
    cp -al "$package" "$NUGET_PACKAGES/"
  done
fi
for framework in net8.0 net10.0; do
  for app in Aot AotWeb; do
    pwsh -NoProfile -File scripts/aot/validate.ps1 \
      -Framework "$framework" -Rid linux-x64 -App "$app" \
      -PackageDirectory /results/packages -Version 2.3.0 -OutputRoot /results/package-validation
  done
done
