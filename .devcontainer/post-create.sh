#!/usr/bin/env bash
set -euo pipefail

echo "Restoring .NET solution..."
dotnet restore AgroGodot.sln

echo "Installing ThreeFrontend npm dependencies..."
npm install --prefix ThreeFrontend

echo "Dev container ready."
