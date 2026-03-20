{ pkgs ? import <nixpkgs> {} }:

pkgs.mkShell {
  buildInputs = [
    pkgs.dotnetCorePackages.dotnet_8.sdk
  ];

  shellHook = ''
    echo "Loaded .NET SDK:"
    dotnet --version
  '';
}
